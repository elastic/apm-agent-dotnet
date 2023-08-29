// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET5_0_OR_GREATER

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		private static readonly string[] ServerPortAttributeKeys = { SemanticConventions.ServerPort, SemanticConventions.NetPeerPort };
		private static readonly string[] ServerAddressAttributeKeys = { SemanticConventions.ServerAddress, SemanticConventions.NetPeerName, SemanticConventions.NetPeerIp };

		private readonly ConditionalWeakTable<Activity, Span> _activeSpans = new();
		private readonly ConditionalWeakTable<Activity, Transaction> _activeTransactions = new();

		internal ElasticActivityListener(IApmAgent agent, HttpTraceConfiguration httpTraceConfiguration) => (_logger, _httpTraceConfiguration) =
			(agent.Logger?.Scoped(nameof(ElasticActivityListener)), httpTraceConfiguration);

		private readonly IApmLogger _logger;
		private Tracer _tracer;
		private readonly HttpTraceConfiguration _httpTraceConfiguration;

		private bool _disposed;

		internal void Start(Tracer tracerInternal)
		{
			_httpTraceConfiguration?.AddTracer(new ElasticSearchHttpNonTracer());
			_tracer = tracerInternal;


			Listener = new ActivityListener
			{
				ActivityStarted = ActivityStarted,
				ActivityStopped = ActivityStopped,
				ShouldListenTo = _ => true,
				Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
			};
			ActivitySource.AddActivityListener(Listener);
		}

		private ActivityListener Listener { get; set; }

		private Action<Activity> ActivityStarted =>
			activity =>
			{
				_logger.Trace()?.Log($"ActivityStarted: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				var spanLinks = new List<SpanLink>(activity.Links.Count());
				if (activity.Links.Any())
				{
					foreach (var link in activity.Links)
						spanLinks.Add(new SpanLink(link.Context.SpanId.ToString(), link.Context.TraceId.ToString()));
				}

				var timestamp = TimeUtils.ToTimestamp(activity.StartTimeUtc);
				if (!CreateTransactionForActivity(activity, timestamp, spanLinks))
					CreateSpanForActivity(activity, timestamp, spanLinks);

			};

		private bool CreateTransactionForActivity(Activity activity, long timestamp, List<SpanLink> spanLinks)
		{
			Transaction transaction = null;
			if (activity.ParentId != null && _tracer.CurrentTransaction == null)
			{
				var dt = TraceContext.TryExtractTracingData(activity.ParentId, activity.Context.TraceState);

				transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
					timestamp, true, activity.SpanId.ToString(),
					distributedTracingData: dt, links: spanLinks, current: activity);
			}
			else if (activity.ParentId == null)
			{
				transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
					timestamp, true, activity.SpanId.ToString(),
					activity.TraceId.ToString(), links: spanLinks, current: activity);
			}

			if (transaction == null) return false;

			transaction.Otel = new OTel { SpanKind = activity.Kind.ToString() };

			if (activity.Id != null)
				_activeTransactions.AddOrUpdate(activity, transaction);
			return true;
		}

		private void CreateSpanForActivity(Activity activity, long timestamp, List<SpanLink> spanLinks)
		{
			Span newSpan;
			if (_tracer.CurrentSpan == null)
			{
				newSpan = (_tracer.CurrentTransaction as Transaction)?.StartSpanInternal(activity.DisplayName, "unknown",
					timestamp: timestamp, id: activity.SpanId.ToString(), links: spanLinks, current: activity);
			}
			else
			{
				newSpan = (_tracer.CurrentSpan as Span)?.StartSpanInternal(activity.DisplayName, "unknown",
					timestamp: timestamp, id: activity.SpanId.ToString(), links: spanLinks, current: activity);
			}

			if (newSpan == null) return;

			newSpan.Otel = new OTel { SpanKind = activity.Kind.ToString() };

			if (activity.Kind == ActivityKind.Internal)
			{
				newSpan.Type = "app";
				newSpan.Subtype = "internal";
			}

			if (activity.Id != null)
				_activeSpans.AddOrUpdate(activity, newSpan);
		}

		private Action<Activity> ActivityStopped =>
			activity =>
			{
				if (activity == null)
				{
					_logger.Trace()?.Log("ActivityStopped called with `null` activity. Ignoring `null` activity.");
					return;
				}
				activity.Stop();

				_logger.Trace()?.Log($"ActivityStopped: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				if (activity.Id == null) return;

				if (_activeTransactions.TryGetValue(activity, out var transaction))
				{
					_activeTransactions.Remove(activity);
					transaction.Duration = activity.Duration.TotalMilliseconds;

					if (activity.TagObjects.Any())
						transaction.Otel.Attributes = new Dictionary<string, object>();

					foreach (var tag in activity.TagObjects)
						transaction.Otel.Attributes.Add(tag.Key, tag.Value);

					InferTransactionType(transaction, activity);

					// By default we set unknown outcome
					transaction.Outcome = Outcome.Unknown;
#if NET6_0_OR_GREATER
					switch (activity.Status)
					{
						case ActivityStatusCode.Unset:
							transaction.Outcome = Outcome.Unknown;
							break;
						case ActivityStatusCode.Ok:
							transaction.Outcome = Outcome.Success;
							break;
						case ActivityStatusCode.Error:
							transaction.Outcome = Outcome.Failure;
							break;
					}
#endif

					transaction.End();
				}
				else if (_activeSpans.TryGetValue(activity, out var span))
				{
					_activeSpans.Remove(activity);
					UpdateSpan(activity, span);
				}
			};

		private static void UpdateSpan(Activity activity, Span span)
		{
			span.Duration = activity.Duration.TotalMilliseconds;

			if (activity.TagObjects.Any())
				span.Otel.Attributes = new Dictionary<string, object>();

			foreach (var tag in activity.TagObjects)
				span.Otel.Attributes.Add(tag.Key, tag.Value);

			InferSpanTypeAndSubType(span, activity);

			// By default we set unknown outcome
			span.Outcome = Outcome.Unknown;
#if NET6_0_OR_GREATER
			switch (activity.Status)
			{
				case ActivityStatusCode.Unset:
					span.Outcome = Outcome.Unknown;
					break;
				case ActivityStatusCode.Ok:
					span.Outcome = Outcome.Success;
					break;
				case ActivityStatusCode.Error:
					span.Outcome = Outcome.Failure;
					break;
			}
#endif
			span.End();
		}

		internal static void UpdateSpanBenchmark(Activity activity, Span span) => UpdateSpan(activity, span);

		private static void InferTransactionType(Transaction transaction, Activity activity)
		{
			var isRpc = activity.Tags.Any(n => n.Key == SemanticConventions.RpcSystem);
			var isHttp = activity.Tags.Any(n => n.Key == SemanticConventions.HttpUrl || n.Key == SemanticConventions.UrlFull || n.Key == SemanticConventions.HttpScheme);
			var isMessaging = activity.Tags.Any(n => n.Key == SemanticConventions.MessagingSystem);

			if (activity.Kind == ActivityKind.Server && (isRpc || isHttp))
				transaction.Type = ApiConstants.TypeRequest;
			else if (activity.Kind == ActivityKind.Consumer && isMessaging)
				transaction.Type = ApiConstants.TypeMessaging;
			else
				transaction.Type = "unknown";
		}

		private static void InferSpanTypeAndSubType(Span span, Activity activity)
		{
			static string HttpPortFromScheme(string scheme)
			{
				return scheme switch
				{
					"http" => "80",
					"https" => "443",
					_ => string.Empty
				};
			}

			// extracts 'host' or 'host:port' from URL
			static string ParseNetName(string url)
			{
				try
				{
					var u = new Uri(url); // https://developer.mozilla.org/en-US/docs/Web/API/URL
					return u.Host + ':' + u.Port;
				}
				catch
				{
					return string.Empty;
				}
			}

			static string ToResourceName(string type, string name)
			{
				return string.IsNullOrEmpty(name) ? type : $"{type}/{name}";
			}

			var peerPort = string.Empty;
			var netName = string.Empty;

			if (TryGetStringValue(activity, ServerPortAttributeKeys, out var netPortValue))
				peerPort = netPortValue;

			if (TryGetStringValue(activity, ServerAddressAttributeKeys, out var netNameValue))
				netName = netNameValue;

			if (netName.Length > 0 && peerPort.Length > 0)
			{
				netName += ':';
				netName += peerPort;
			}

			string serviceTargetType = null;
			string serviceTargetName = null;
			string resource = null;

			var isDbSpan = TryGetStringValue(activity, SemanticConventions.DbSystem, out var dbSystem);

			if (isDbSpan)
			{
				span.Type = ApiConstants.TypeDb;
				span.Subtype = dbSystem;
				serviceTargetType = span.Subtype;
				serviceTargetName = TryGetStringValue(activity, SemanticConventions.DbName, out var dbName) ? dbName : null;
				resource = ToResourceName(span.Subtype, serviceTargetName);
			}
			else if (activity.Tags.Any(n => n.Key == SemanticConventions.MessagingSystem))
			{
				span.Type = ApiConstants.TypeMessaging;
				span.Subtype = activity.Tags.First(n => n.Key == SemanticConventions.MessagingSystem).Value;
				serviceTargetType = span.Subtype;
				serviceTargetName = activity.Tags.FirstOrDefault(n => n.Key == SemanticConventions.MessagingDestination).Value;
				resource = ToResourceName(span.Subtype, serviceTargetName);
			}
			else if (activity.Tags.Any(n => n.Key == SemanticConventions.RpcSystem))
			{
				span.Type = ApiConstants.TypeExternal;
				span.Subtype = activity.Tags.First(n => n.Key == SemanticConventions.RpcSystem).Value;
				serviceTargetType = span.Subtype;
				serviceTargetName = !string.IsNullOrEmpty(netName)
					? netName
					: activity.Tags.FirstOrDefault(n => n.Key == SemanticConventions.RpcService).Value;
				resource = serviceTargetName ?? span.Subtype;
			}
			else if (activity.Tags.Any(n => n.Key == SemanticConventions.HttpUrl || n.Key == SemanticConventions.UrlFull || n.Key == SemanticConventions.HttpScheme))
			{
				span.Type = ApiConstants.TypeExternal;
				span.Subtype = ApiConstants.SubtypeHttp;
				serviceTargetType = span.Subtype;
				if (activity.Tags.Any(n => n.Key == SemanticConventions.HttpHost) && activity.Tags.Any(n => n.Key == SemanticConventions.HttpScheme))
				{
					serviceTargetName = activity.Tags.FirstOrDefault(n => n.Key == SemanticConventions.HttpHost).Value + ":"
						+ HttpPortFromScheme(activity.Tags.FirstOrDefault(n => n.Key == SemanticConventions.HttpScheme).Value);
				}
				else if (activity.Tags.Any(n => n.Key == SemanticConventions.HttpUrl))
					serviceTargetName = ParseNetName(activity.Tags.FirstOrDefault(n => n.Key == SemanticConventions.HttpUrl).Value);
				else if (activity.Tags.Any(n => n.Key == SemanticConventions.UrlFull))
					serviceTargetName = ParseNetName(activity.Tags.FirstOrDefault(n => n.Key == SemanticConventions.UrlFull).Value);
				else
					serviceTargetName = netName;

				resource = serviceTargetName;
			}

			if (serviceTargetType == null)
			{
				if (activity.Kind == ActivityKind.Internal)
				{
					span.Type = ApiConstants.TypeApp;
					span.Subtype = ApiConstants.SubTypeInternal;
				}
				else
					span.Type = ApiConstants.TypeUnknown;
			}

			span.Context.Service = new SpanService(new Target(serviceTargetType, serviceTargetName));
			if (resource != null)
			{
				span.Context.Destination ??= new Destination();
				span.Context.Destination.Service = new Destination.DestinationService { Resource = resource };
			}
		}

		private static bool TryGetStringValue(Activity activity, string key, out string value)
		{
			value = null;

			var attribute = activity.GetTagItem(key);

			if (attribute is string stringValue)
			{
				value = stringValue;
				return true;
			}

			if (attribute is int intValue)
			{
				value = intValue.ToString();
				return true;
			}

			return false;
		}

		private static bool TryGetStringValue(Activity activity, string[] keys, out string value)
		{
			value = null;

			foreach (var key in keys)
			{
				if (TryGetStringValue(activity, key, out var attributeValue))
				{
					value = attributeValue;
					return true;
				}
			}

			return false;
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
					Listener?.Dispose();

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
