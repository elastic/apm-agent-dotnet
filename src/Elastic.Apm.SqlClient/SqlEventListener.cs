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
		private readonly ApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<int, (Span Span, long Start)> _processingSpans = new ConcurrentDictionary<int, (Span, long)>();

		public SqlEventListener(IApmAgent apmAgent)
		{
			_apmAgent = (ApmAgent)apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(SqlEventListener));
		}

		protected override void OnEventSourceCreated(EventSource eventSource)
		{
			if (eventSource != null && eventSource.Name == "Microsoft-AdoNet-SystemData"
				&& eventSource.GetType().FullName == "System.Data.SqlEventSource")
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
				_logger?.Error()?.LogException(ex, "Error has occurred during handle event from SqlClient. EventData: {EventData}", eventData);
			}
		}

		private void ProcessBeginExecute(IReadOnlyList<object> payload)
		{
			if (payload.Count != 4)
			{
				_logger?.Debug()
					?.Log("BeginExecute event has {PayloadCount} payload items instead of 4. Event processing is skipped.", payload.Count);
				return;
			}

			var id = Convert.ToInt32(payload[0]);
			var dataSource = Convert.ToString(payload[1]);
			var database = Convert.ToString(payload[2]);
			var commandText = Convert.ToString(payload[3]);
			var start = Stopwatch.GetTimestamp();

			_logger?.Trace()
				?.Log("Process BeginExecute event. Id: {Id}. Data source: {DataSource}. Database: {Database}. CommandText: {CommandText}.", id,
					dataSource, database, commandText);

			var spanName = !string.IsNullOrWhiteSpace(commandText)
				? commandText.Replace(Environment.NewLine, "")
				: database;

			var span = (Span)ExecutionSegmentCommon.GetCurrentExecutionSegment(_apmAgent)?.StartSpan(spanName, ApiConstants.TypeDb);
			if (span == null) return;

			if (_processingSpans.TryAdd(id, (span, start)))
			{
				span.Context.Db = new Database
				{
					Statement = !string.IsNullOrWhiteSpace(commandText)
						? commandText.Replace(Environment.NewLine, "")
						: string.Empty,
					Instance = database,
					Type = Database.TypeSql
				};

				span.Context.Destination = _apmAgent.TracerInternal.DbSpanCommon.GetDestination($"Data Source={dataSource}", false, null);

				// At the moment only System.Data.SqlClient and Microsoft.Data.SqlClient spread events via EventSource with Microsoft-AdoNet-SystemData name
				span.Subtype = ApiConstants.SubtypeMssql;
			}
		}

		private void ProcessEndExecute(IReadOnlyList<object> payload)
		{
			if (payload.Count != 3)
			{
				_logger?.Debug()
					?.Log("EndExecute event has {PayloadCount} payload items instead of 3. Event processing is skipped.", payload.Count);
				return;
			}

			var id = Convert.ToInt32(payload[0]);
			var compositeState = Convert.ToInt32(payload[1]);
			var sqlExceptionNumber = Convert.ToInt32(payload[2]);
			var stop = Stopwatch.GetTimestamp();

			_logger?.Trace()
				?.Log("Process EndExecute event. Id: {Id}. Composite state: {CompositeState}. Sql exception number: {SqlExceptionNumber}.", id,
					compositeState, sqlExceptionNumber);

			if (!_processingSpans.TryGetValue(id, out var item))
			{
				_logger?.Warning()
					?.Log("Failed capturing sql statement (failed to remove from ProcessingSpans).");
				return;
			}

			var isSuccess = (compositeState & 1) == 1;
			var isSqlException = (compositeState & 2) == 2;
			// 4 - is synchronous

			item.Span.Duration = ((stop - item.Start) / (double)Stopwatch.Frequency) * 1000;

			if (isSqlException)
			{
				item.Span.CaptureError("Exception has occurred", sqlExceptionNumber != 0 ? $"SQL Exception {sqlExceptionNumber}" : null,
					null);
			}

			item.Span.End();
		}
	}
}
