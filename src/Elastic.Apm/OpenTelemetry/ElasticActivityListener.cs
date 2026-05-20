// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET || NETSTANDARD2_1
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.OpenTelemetry
{
	public class ElasticActivityListener : IDisposable
	{
		private readonly ConditionalWeakTable<Activity, Span> _activeSpans = new();
		private readonly ConditionalWeakTable<Activity, Transaction> _activeTransactions = new();
		private readonly IApmAgent _agent;
		private readonly IApmLogger _logger;
		private volatile Tracer _tracer;
		private ActivityListener _listener;

		private volatile bool _hasServiceBusInstrumentation;
		private volatile bool _hasStorageInstrumentation;
		private volatile bool _hasCosmosDbInstrumentation;
		private volatile bool _hasMongoDbInstrumentation;
		private volatile bool _hasGrpcClientInstrumentation;

		private bool _disposed;

		internal ElasticActivityListener(IApmAgent agent)
		{
			_agent = agent;
			_logger = agent.Logger?.Scoped(nameof(ElasticActivityListener));
		}

		internal void Start(Tracer tracerInternal)
		{
			_tracer = tracerInternal;

			if (_listener != null)
				return;

			// Subscribe before scanning to avoid missing a load that races with the initial scan
			AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
				CheckAssembly(assembly.GetName().Name);

			_logger?.Debug()?.Log(
				"ElasticActivityListener started. Detected instrumentation packages: ServiceBus={ServiceBus}, Storage={Storage}, " +
				"CosmosDb={CosmosDb}, MongoDb={MongoDb}, GrpcClient={GrpcClient}",
				_hasServiceBusInstrumentation, _hasStorageInstrumentation, _hasCosmosDbInstrumentation,
				_hasMongoDbInstrumentation, _hasGrpcClientInstrumentation);

			_listener = new ActivityListener
			{
				ActivityStarted = OnActivityStarted,
				ActivityStopped = OnActivityStopped,
				ShouldListenTo = _ => true,
				Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
			};

			ActivitySource.AddActivityListener(_listener);
		}

		private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args) =>
			CheckAssembly(args.LoadedAssembly.GetName().Name);

		internal void CheckAssembly(string name)
		{
			if (name == "Elastic.Apm.Azure.ServiceBus")
			{
				_hasServiceBusInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.Azure.ServiceBus' — 'Microsoft.ServiceBus' activities will be skipped by the OTel bridge.");
			}
			else if (name == "Elastic.Apm.Azure.Storage")
			{
				_hasStorageInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.Azure.Storage' — 'Microsoft.Storage' activities will be skipped by the OTel bridge.");
			}
			else if (name == "Elastic.Apm.Azure.CosmosDb")
			{
				_hasCosmosDbInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.Azure.CosmosDb' — 'Microsoft.DocumentDB' activities will be skipped by the OTel bridge.");
			}
			else if (name == "Elastic.Apm.MongoDb")
			{
				_hasMongoDbInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.MongoDb' — 'MongoDB.Driver' activities will be skipped by the OTel bridge.");
			}
			else if (name == "Elastic.Apm.GrpcClient")
			{
				_hasGrpcClientInstrumentation = true;
				_logger?.Debug()?.Log("Detected 'Elastic.Apm.GrpcClient' — 'Grpc.Net.Client' activities will be skipped by the OTel bridge.");
			}
		}

		private void OnActivityStarted(Activity activity)
		{
			if (_tracer == null)
				return;

			var config = _agent.Configuration;
			if (!config.Enabled || !config.Recording)
				return;

			if (ShouldSkipActivity(activity, out var skipReason))
			{
				LogSkippedActivity(activity, skipReason, onStart: true);
				return;
			}

			_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} traceId:{TraceId}",
				activity.DisplayName, activity.Id, activity.TraceId);

			List<SpanLink> spanLinks = null;
			foreach (var link in activity.Links)
			{
				spanLinks ??= [];
				spanLinks.Add(new SpanLink(link.Context.SpanId.ToString(), link.Context.TraceId.ToString()));
			}

			var timestamp = TimeUtils.ToTimestamp(activity.StartTimeUtc);
			if (!CreateTransactionForActivity(activity, timestamp, spanLinks))
				CreateSpanForActivity(activity, timestamp, spanLinks);
		}

		/// <summary>
		/// Central policy for activities the OTel bridge must not capture (dedup, known listeners, broken upstream sources).
		/// Used by both <see cref="OnActivityStarted"/> and <see cref="OnActivityStopped"/> so start/stop stay symmetric.
		/// </summary>
		private bool ShouldSkipActivity(Activity activity, out ActivitySkipReason skipReason)
		{
			skipReason = ActivitySkipReason.None;

			// Prevent recording of Azure Functions activities which are quite broken at the moment
			// See https://github.com/Azure/azure-functions-dotnet-worker/issues/2733
			// See https://github.com/Azure/azure-functions-dotnet-worker/issues/2875
			// See https://github.com/Azure/azure-functions-host/issues/10641
			// See https://github.com/Azure/azure-functions-dotnet-worker/issues/2810
			if ((activity.Source.Name == "" && activity.DisplayName == "InvokeFunctionAsync")
				|| activity.Source.Name == "Microsoft.Azure.Functions.Worker")
			{
				skipReason = ActivitySkipReason.AzureFunctions;
				return true;
			}

			if (KnownListeners.SkippedActivityNamesSet.Contains(activity.OperationName))
			{
				skipReason = ActivitySkipReason.KnownListener;
				return true;
			}

			// If the Elastic instrumentation for an Azure service is present, skip duplicating through the OTel bridge.
			// Guard with a source name prefix check to avoid the tag lookup on non-Azure activities.
			if ((_hasServiceBusInstrumentation || _hasStorageInstrumentation || _hasCosmosDbInstrumentation)
				&& (activity.Source.Name.StartsWith("Azure.", StringComparison.Ordinal)
					|| activity.Source.Name.StartsWith("Microsoft.Azure.", StringComparison.Ordinal)))
			{
				OTelActivityMapper.TryGetStringValue(activity, SemanticConventions.AzNamespace, out var azNamespace);

				if (_hasServiceBusInstrumentation && azNamespace == "Microsoft.ServiceBus")
				{
					skipReason = ActivitySkipReason.ServiceBusDedup;
					return true;
				}

				if (_hasStorageInstrumentation && azNamespace == "Microsoft.Storage")
				{
					skipReason = ActivitySkipReason.StorageDedup;
					return true;
				}

				if (_hasCosmosDbInstrumentation && azNamespace == "Microsoft.DocumentDB")
				{
					skipReason = ActivitySkipReason.CosmosDbDedup;
					return true;
				}
			}

			if (_hasMongoDbInstrumentation && activity.Source.Name == "MongoDB.Driver")
			{
				skipReason = ActivitySkipReason.MongoDbDedup;
				return true;
			}

			if (_hasGrpcClientInstrumentation && activity.Source.Name == "Grpc.Net.Client")
			{
				skipReason = ActivitySkipReason.GrpcClientDedup;
				return true;
			}

			return false;
		}

		private void LogSkippedActivity(Activity activity, ActivitySkipReason skipReason, bool onStart)
		{
			var phase = onStart ? "ActivityStarted" : "ActivityStopped";

			switch (skipReason)
			{
				case ActivitySkipReason.AzureFunctions:
					_logger?.Trace()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} skipped Azure Functions activity " +
						"(source='{SourceName}') due to known upstream issues.", phase, activity.DisplayName, activity.Id, activity.Source.Name);
					break;
				case ActivitySkipReason.ServiceBusDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Microsoft.ServiceBus' " +
						"activity because 'Elastic.Apm.Azure.ServiceBus' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.StorageDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Microsoft.Storage' " +
						"activity because 'Elastic.Apm.Azure.Storage' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.CosmosDbDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Microsoft.DocumentDB' " +
						"activity because 'Elastic.Apm.Azure.CosmosDb' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.MongoDbDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'MongoDB.Driver' " +
						"activity because 'Elastic.Apm.MongoDb' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.GrpcClientDedup:
					_logger?.Debug()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped 'Grpc.Net.Client' " +
						"activity because 'Elastic.Apm.GrpcClient' is present in the application.",
						phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
				case ActivitySkipReason.KnownListener:
					_logger?.Trace()?.Log("{Phase}: name:{DisplayName} id:{ActivityId} traceId:{TraceId} skipped because it matched " +
						"a skipped activity name defined in KnownListeners.", phase, activity.DisplayName, activity.Id, activity.TraceId);
					break;
			}
		}

		private enum ActivitySkipReason
		{
			None,
			AzureFunctions,
			ServiceBusDedup,
			StorageDedup,
			CosmosDbDedup,
			MongoDbDedup,
			GrpcClientDedup,
			KnownListener
		}

		private bool CreateTransactionForActivity(Activity activity, long timestamp, List<SpanLink> spanLinks)
		{
			Transaction transaction = null;
			if (activity.ParentId != null && _tracer.CurrentTransaction == null)
			{
				var dt = TraceContext.TryExtractTracingData(activity.ParentId, activity.Context.TraceState);

				transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
					timestamp, true, activity.SpanId.ToString(),
					distributedTracingData: dt, links: spanLinks?.Count > 0 ? spanLinks : null, current: activity);
			}
			else if (activity.ParentId == null && _tracer.CurrentTransaction == null)
			{
				transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
					timestamp, true, activity.SpanId.ToString(),
					activity.TraceId.ToString(), links: spanLinks?.Count > 0 ? spanLinks : null, current: activity);
			}

			if (transaction == null)
				return false;

			transaction.Otel = new OTel { SpanKind = activity.Kind.ToString() };

			_activeTransactions.AddOrUpdate(activity, transaction);

			_logger?.Trace()?.Log("Created transaction id:{TransactionId} name:{Name} for activity id:{ActivityId}",
				transaction.Id, transaction.Name, activity.Id);

			return true;
		}

		private void CreateSpanForActivity(Activity activity, long timestamp, List<SpanLink> spanLinks)
		{
			Span newSpan;
			if (_tracer.CurrentSpan == null)
			{
				newSpan = (_tracer.CurrentTransaction as Transaction)?.StartSpanInternal(activity.DisplayName, "unknown",
					timestamp: timestamp, id: activity.SpanId.ToString(), links: spanLinks?.Count > 0 ? spanLinks : null, current: activity);
			}
			else
			{
				newSpan = (_tracer.CurrentSpan as Span)?.StartSpanInternal(activity.DisplayName, "unknown",
					timestamp: timestamp, id: activity.SpanId.ToString(), links: spanLinks?.Count > 0 ? spanLinks : null, current: activity);
			}

			if (newSpan == null)
			{
				_logger?.Trace()?.Log("ActivityStarted: name:{DisplayName} id:{ActivityId} — could not create span (parent transaction/span is absent or non-recording). Activity will not be captured.",
					activity.DisplayName, activity.Id);
				return;
			}

			newSpan.Otel = new OTel { SpanKind = activity.Kind.ToString() };

			if (activity.Kind == ActivityKind.Internal)
			{
				newSpan.Type = "app";
				newSpan.Subtype = "internal";
			}

			_activeSpans.AddOrUpdate(activity, newSpan);

			_logger?.Trace()?.Log("Created span id:{SpanId} name:{Name} for activity id:{ActivityId}",
				newSpan.Id, newSpan.Name, activity.Id);
		}

		private void OnActivityStopped(Activity activity)
		{
			if (activity == null)
			{
				_logger?.Trace()?.Log("ActivityStopped called with `null` activity. Ignoring `null` activity.");
				return;
			}

			if (_tracer == null)
				return;

			var config = _agent.Configuration;
			if (!config.Enabled || !config.Recording)
				return;

			if (ShouldSkipActivity(activity, out _))
				return;

			_logger?.Trace()?.Log("ActivityStopped: name:{DisplayName} id:{ActivityId} traceId:{TraceId}",
				activity.DisplayName, activity.Id, activity.TraceId);

			if (_activeTransactions.TryGetValue(activity, out var transaction))
			{
				_activeTransactions.Remove(activity);
				transaction.Duration = activity.Duration.TotalMilliseconds;

				OTelActivityMapper.UpdateOTelAttributes(activity, transaction.Otel);
				OTelActivityMapper.InferTransactionType(transaction, activity);

				transaction.Outcome = Outcome.Unknown;
#if NET // Not available in netstandard2.1
				transaction.Outcome = ActivityStatusToOutcome(activity.Status);
#endif
				transaction.End();
			}
			else if (_activeSpans.TryGetValue(activity, out var span))
			{
				_activeSpans.Remove(activity);
				UpdateSpan(activity, span);
			}
			else
			{
				_logger?.Trace()?.Log("ActivityStopped: name:{DisplayName} id:{ActivityId} — activity was not tracked (no matching span or transaction).",
					activity.DisplayName, activity.Id);
			}
		}

		private static void UpdateSpan(Activity activity, Span span)
		{
			span.Duration = activity.Duration.TotalMilliseconds;

			OTelActivityMapper.UpdateOTelAttributes(activity, span.Otel);
			OTelActivityMapper.InferSpanTypeAndSubType(span, activity);

			span.Outcome = Outcome.Unknown;
#if NET // Not available in netstandard2.1
			span.Outcome = ActivityStatusToOutcome(activity.Status);
#endif
			span.End();
		}

		/// <summary>
		/// Specifically exposed for benchmarking. This is not intended for any other purpose.
		/// </summary>
		internal static void UpdateSpanBenchmark(Activity activity, Span span) => UpdateSpan(activity, span);

#if NET
		private static Outcome ActivityStatusToOutcome(ActivityStatusCode status) => status switch
		{
			ActivityStatusCode.Ok    => Outcome.Success,
			ActivityStatusCode.Error => Outcome.Failure,
			_                        => Outcome.Unknown
		};
#endif

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_logger?.Debug()?.Log("ElasticActivityListener disposing.");
					AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
					_listener?.Dispose();
					_listener = null;
				}

				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
#endif
