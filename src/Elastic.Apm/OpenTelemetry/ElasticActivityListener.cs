// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#if NET5_0

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.OpenTelemetry
{
	public class ElasticActivityListener
	{
		internal readonly ConcurrentDictionary<string, Span> ActiveSpans = new();
		internal readonly ConcurrentDictionary<string, Transaction> ActiveTransactions = new();

		internal ElasticActivityListener(IApmAgent agent, Tracer tracerInternal)
		{
			_logger = agent.Logger?.Scoped(nameof(ElasticActivityListener));
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

		private readonly IApmLogger _logger;
		internal Tracer _tracer;


		private Action<Activity> ActivityStarted =>
			activity =>
			{
				_logger.Trace()?.Log($"ActivityStarted: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				Transaction transaction = null;
				if (activity.Context != null && activity.Context.IsRemote && activity.ParentId != null)
				{
					var dt = TraceContext.TryExtractTracingData(activity.ParentId.ToString(), activity.Context.TraceState);

					transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
						TimeUtils.ToTimestamp(activity.StartTimeUtc), true, activity.SpanId.ToString(),
						distributedTracingData: dt);
				}
				else if (activity.ParentId == null)
				{
					transaction = _tracer.StartTransactionInternal(activity.DisplayName, "unknown",
						TimeUtils.ToTimestamp(activity.StartTimeUtc), true, activity.SpanId.ToString(),
						activity.TraceId.ToString());
				}
				else
				{
					Span newSpan;
					if (_tracer.CurrentSpan == null)
					{
						newSpan = (_tracer.CurrentTransaction as Transaction)?.StartSpanInternal(activity.DisplayName, "unknown",
							timestamp: TimeUtils.ToTimestamp(activity.StartTimeUtc), id: activity.SpanId.ToString());
					}
					else
					{
						newSpan = (_tracer.CurrentSpan as Span)?.StartSpanInternal(activity.DisplayName, "unknown",
							timestamp: TimeUtils.ToTimestamp(activity.StartTimeUtc), id: activity.SpanId.ToString());
					}

					if (newSpan != null)
					{
						newSpan.Otel = new OTel();
						newSpan.Otel.SpanKind = activity.Kind.ToString();

						if (activity.Id != null) ActiveSpans.TryAdd(activity.Id, newSpan);
					}
				}

				if (transaction != null)
				{
					transaction.Otel = new OTel();
					transaction.Otel.SpanKind = activity.Kind.ToString();

					if (activity.Kind == ActivityKind.Server)
						transaction.Type = ApiConstants.TypeRequest;
					else if (activity.Kind == ActivityKind.Consumer) transaction.Type = ApiConstants.TypeMessaging;

					if (activity.Id != null) ActiveTransactions.TryAdd(activity.Id, transaction);
				}
			};


		private Action<Activity> ActivityStopped =>
			activity =>
			{
				if (activity == null)
				{
					_logger.Trace()?.Log("ActivityStopped called with `null` activity. Ignoring `null` activity.");
					return;
				}

				_logger.Trace()?.Log($"ActivityStopped: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				if (activity.Id != null)
				{
					if (ActiveTransactions.TryRemove(activity.Id, out var transaction))
					{
						transaction.Duration = activity.Duration.TotalMilliseconds;

						if (activity.Tags.Any())
							transaction.Otel.Attributes = new Dictionary<string, string>();

						foreach (var tag in activity.Tags) transaction.Otel.Attributes.Add(tag.Key, tag.Value);
						transaction.End();
					}
					else if (ActiveSpans.TryRemove(activity.Id, out var span))
					{
						span.Duration = activity.Duration.TotalMilliseconds;

						if (activity.Tags.Any())
							span.Otel.Attributes = new Dictionary<string, string>();

						foreach (var tag in activity.Tags) span.Otel.Attributes.Add(tag.Key, tag.Value);

						InferSpanTypeAndSubType(span, activity);
						span.End();
					}
				}
			};

		public ActivityListener Listener { get; }

		private void InferSpanTypeAndSubType(Span span, Activity activity)
		{
			if (activity.Kind == ActivityKind.Client)
			{
				if (activity.Tags.Any(n => n.Key == "http.url" || n.Key == "http.scheme"))
				{
					span.Type = ApiConstants.TypeExternal;
					span.Subtype = ApiConstants.SubtypeHttp;
				}
				else if (activity.Tags.Any(n => n.Key == "db.system"))
				{
					span.Type = ApiConstants.TypeDb;
					span.Subtype = activity.Tags.First(n => n.Key == "db.system").Value;
				}
				else if (activity.Tags.Any(n => n.Key == "rpc.system"))
				{
					span.Type = ApiConstants.TypeExternal;
					span.Subtype = activity.Tags.First(n => n.Key == "rpc.system").Value;
				}
				else if (activity.Kind == ActivityKind.Client)
				{
					span.Type = ApiConstants.TypeMessaging;
					span.Subtype = activity.Tags.First(n => n.Key == "messaging.system").Value;
				}
			}
			else if (activity.Kind == ActivityKind.Consumer)
			{
				if (activity.Tags.Any(n => n.Key == "messaging.system"))
				{
					span.Type = ApiConstants.TypeMessaging;
					span.Subtype = activity.Tags.First(n => n.Key == "messaging.system").Value;
				}
			}
		}
	}
}

#endif
