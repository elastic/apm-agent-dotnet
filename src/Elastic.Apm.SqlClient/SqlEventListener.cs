using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.SqlClient
{
	internal class SqlEventListener : EventListener
	{
		private readonly IApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<int, (Span Span, long Start)> _spans = new ConcurrentDictionary<int, (Span, long)>();

		public SqlEventListener(IApmAgent apmAgent)
		{
			_apmAgent = apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(SqlEventListener));
		}

		protected override void OnEventSourceCreated(EventSource eventSource)
		{
			if (eventSource != null && eventSource.Name == "Microsoft-AdoNet-SystemData")
			{
				EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)1);
			}

			base.OnEventSourceCreated(eventSource);
		}

		private const int BeginExecuteEventId = 1;
		private const int EndExecuteId = 2;

		protected override void OnEventWritten(EventWrittenEventArgs eventData)
		{
			if (eventData?.Payload == null) return;

			try
			{
				switch (eventData.EventId)
				{
					case BeginExecuteEventId:
						ProcessBeginExecute(eventData.Payload);
						break;
					case EndExecuteId:
						ProcessEndExecute(eventData.Payload);
						break;
				}
			}
			catch (Exception ex)
			{
				_logger?.Error()?.LogException(ex, "Error was occurred during handle event from SqlClient. EventData: {@EventData}", eventData);
			}
		}

		private void ProcessBeginExecute(IReadOnlyList<object> payload)
		{
			string s = null;
			if (payload.Count == 4)
			{
				var id = Convert.ToInt32(payload[0]);
				var datasource = Convert.ToString(payload[1]);
				var database = Convert.ToString(payload[2]);
				var commandText = Convert.ToString(payload[3]);
				var start = Stopwatch.GetTimestamp();

				// todo: let's try to enable Instrumentation Engine and check does it work without AppInsights
				https://docs.microsoft.com/en-us/azure/azure-monitor/app/asp-net-dependencies#advanced-sql-tracking-to-get-full-sql-query
				var spanName = !string.IsNullOrWhiteSpace(commandText)
					? commandText.Replace(Environment.NewLine, "")
					// todo: what we need to use here
					: database;

				var span = (Span)ExecutionSegmentCommon.GetCurrentExecutionSegment(_apmAgent).StartSpan(spanName, ApiConstants.TypeDb);

				if (_spans.TryAdd(id, (span, start)))
				{
					span.Context.Db = new Database
					{
						Statement = !string.IsNullOrWhiteSpace(commandText)
							? commandText.Replace(Environment.NewLine, "")
							: string.Empty,
						Instance = database,
						Type = Database.TypeSql
					};

					// todo: destination

					// todo: check provider types. Do they can spread events via EventSource?
				}
			}
		}

		private void ProcessEndExecute(IReadOnlyList<object> payload)
		{
			if (payload.Count == 3)
			{
				var id = Convert.ToInt32(payload[0]);
				var compositeState = Convert.ToInt32(payload[1]);
				var sqlExceptionNumber = Convert.ToInt32(payload[2]);
				var stop = Stopwatch.GetTimestamp();

				if (_spans.TryGetValue(id, out var item))
				{
					// todo: enrich span result
					// todo: check sqlExceptionNumber

					item.Span.Duration = ((stop - item.Start) / (double)Stopwatch.Frequency) * 1000;

					item.Span.End();
				}
			}
		}
	}
}
