// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;

namespace Elastic.Apm.OpenTelemetry
{
	public class ElasticActivityListener
	{
		public ActivityListener listener { get; }
		internal ApmAgent _agent;
		internal readonly ConcurrentDictionary<string, Transaction> ActiveTransactions = new ConcurrentDictionary<string, Transaction>();
		internal readonly ConcurrentDictionary<string, Span> ActiveSpans = new ConcurrentDictionary<string, Span>();

		private Action<Activity>? ActivityStarted =>
			activity =>
			{
				Console.WriteLine($"ActivityStarted: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				if (_agent.Tracer.CurrentTransaction == null)
				{
					var transaction = _agent.TracerInternal.StartTransactionInternal(activity.DisplayName, "Todo",
						timestamp: TimeUtils.ToTimestamp(activity.StartTimeUtc), ignoreActivity: false, id: activity.Id);
					ActiveTransactions.TryAdd(activity.Id, transaction);
				}
				else
				{
					if (_agent.TracerInternal.CurrentSpan == null)
					{
						var newSpan = (_agent.TracerInternal.CurrentTransaction as Transaction)?.StartSpanInternal(activity.DisplayName, "ToDo",
							"ToDo", timestamp: TimeUtils.ToTimestamp(activity.StartTimeUtc));
						ActiveSpans.TryAdd(activity.Id, newSpan);
					}
					else
					{
						var newSpan = (_agent.TracerInternal.CurrentSpan as Span)?.StartSpanInternal(activity.DisplayName, "ToDo",
							"ToDo", timestamp: TimeUtils.ToTimestamp(activity.StartTimeUtc));
						ActiveSpans.TryAdd(activity.Id, newSpan);
					}
				}
			};


		private Action<Activity>? ActivityStopped =>
			new Action<Activity>(activity =>
			{
				Console.WriteLine($"ActivityStopped: name:{activity.DisplayName} id:{activity.Id} traceId:{activity.TraceId}");

				if (KnownListeners.KnownListenersList.Contains(activity.DisplayName))
					return;

				if (ActiveTransactions.TryRemove(activity.Id, out var transaction))
				{
					transaction.Duration = activity.Duration.TotalMilliseconds;
					transaction.End();
				}

				if (ActiveSpans.TryRemove(activity.Id, out var span))
				{
					span.Duration = activity.Duration.TotalMilliseconds;
					span.End();
				}
			});

		internal ElasticActivityListener(ApmAgent agent)
		{
			_agent = agent;
			listener = new ActivityListener()
			{
				ActivityStarted = ActivityStarted,
				ActivityStopped = ActivityStopped,
				ShouldListenTo = a => true,
				Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
			};
			ActivitySource.AddActivityListener(listener);
		}
	}
}
