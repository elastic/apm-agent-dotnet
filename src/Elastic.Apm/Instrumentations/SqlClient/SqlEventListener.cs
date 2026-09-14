// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Instrumentations.SqlClient
{
	internal class SqlEventListener : EventListener
	{
		private const int BeginExecuteEventId = 1;
		private const int EndExecuteId = 2;
		private readonly ApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<int, (ISpan Span, long Start)> _processingSpans = new();

		public SqlEventListener(IApmAgent apmAgent)
		{
			_apmAgent = (ApmAgent)apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(SqlEventListener));
		}

		protected override void OnEventSourceCreated(EventSource eventSource)
		{
			switch (eventSource)
			{
				// `Microsoft-AdoNet-SystemData` used to be emitted by both by System.Data.SqlClient and Microsoft.Data.SqlClient.
				// We only want to listen to it in case it's emitted by `System.Data.SqlEventSource` as we can only subscribe once to a name.
				case { Name: "Microsoft-AdoNet-SystemData" } when eventSource.GetType().FullName == "System.Data.SqlEventSource":
				// Always enable it for the new event source
				// https://github.com/dotnet/SqlClient/issues/436
				case { Name: "Microsoft.Data.SqlClient.EventSource" }:
					EnableEvents(eventSource, EventLevel.Informational, (EventKeywords)1);
					break;
			}

			base.OnEventSourceCreated(eventSource);
		}

		protected override void OnEventWritten(EventWrittenEventArgs eventData)
		{
			if (eventData?.Payload == null)
				return;

			HandleEvent(eventData.EventId, eventData.Payload);
		}

		/// <summary>
		/// Handles a SqlClient EventSource event. Separated from <see cref="OnEventWritten" /> so that it can be exercised
		/// without an <see cref="EventSource" />.
		/// </summary>
		internal void HandleEvent(int eventId, IReadOnlyList<object> payload)
		{
			if (payload == null)
				return;

			try
			{
				switch (eventId)
				{
					case BeginExecuteEventId:
						// The command is already traced by a competing instrumentation (Entity Framework or the profiler's
						// ADO.NET integrations), so don't start a nested span for the very same command. The matching
						// EndExecute event is still handled below and finds nothing to end. Unlike the diagnostic listener
						// this event source carries no command object, so the competing span cannot be matched to the command.
						if (CompetingInstrumentation.IsCommandTracedByOtherModule(_apmAgent))
						{
							_logger?.Trace()?.Log("BeginExecute event is skipped, the command is traced by a competing instrumentation.");
							return;
						}

						ProcessBeginExecute(payload);
						break;
					case EndExecuteId:
						ProcessEndExecute(payload);
						break;
				}
			}
			catch (Exception ex)
			{
				_logger?.Error()?.LogException(ex, "Error has occurred during handle event from SqlClient. EventId: {EventId}", eventId);
			}
		}

		private void ProcessBeginExecute(IReadOnlyList<object> payload)
		{
			// https://github.com/dotnet/SqlClient/blob/3a41288f2b67307a1f816761deb73785247c85c9/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/SqlClientEventSource.cs#L1009
			// SqlClient reserves the first 4 payload items, at the time of writing they've added a 5th (message).
			// int objectId, string dataSource, string database, string commandText, string message
			if (payload.Count < 4)
			{
				_logger?.Debug()
					?.Log("BeginExecute event has {PayloadCount} payload items, expecting at least 4. Event processing is skipped.", payload.Count);
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

			// A SQL command is an exit span that never has legitimate children, so it isn't made the current span. This also
			// guarantees that a current span flagged SqlClient belongs to a competing instrumentation, never to this listener.
			var span = ExecutionSegmentCommon.StartSpanOnCurrentExecutionSegment(_apmAgent, spanName, ApiConstants.TypeDb, ApiConstants.SubtypeMssql,
				InstrumentationFlag.SqlClient, isExitSpan: true, makeCurrent: false);

			if (span == null)
				return;

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

				span.Context.Destination = _apmAgent.TracerInternal.DbSpanCommon.GetDestination($"Data Source={dataSource}", null);

				// At the moment only System.Data.SqlClient and Microsoft.Data.SqlClient spread events via EventSource with Microsoft-AdoNet-SystemData name
				span.Subtype = ApiConstants.SubtypeMssql;
			}
		}

		private void ProcessEndExecute(IReadOnlyList<object> payload)
		{
			// https://github.com/dotnet/SqlClient/blob/3a41288f2b67307a1f816761deb73785247c85c9/src/Microsoft.Data.SqlClient/src/Microsoft/Data/SqlClient/SqlClientEventSource.cs#L1017
			// SqlClient EventSource reserves the first 3 payload items but may extend this further in the future.
			// At the time of writing this event includes an additional 4th (message).
			if (payload.Count < 3)
			{
				_logger?.Debug()
					?.Log("EndExecute event has {PayloadCount} payload items, expecting at least 3. Event processing is skipped.", payload.Count);
				return;
			}

			var id = Convert.ToInt32(payload[0]);
			var compositeState = Convert.ToInt32(payload[1]);
			var sqlExceptionNumber = Convert.ToInt32(payload[2]);
			var stop = Stopwatch.GetTimestamp();

			_logger?.Trace()
				?.Log("Process EndExecute event. Id: {Id}. Composite state: {CompositeState}. Sql exception number: {SqlExceptionNumber}.", id,
					compositeState, sqlExceptionNumber);

			if (!_processingSpans.TryRemove(id, out var item))
			{
				// Expected whenever BeginExecute did not start a span: no current transaction, or the command is traced by a
				// competing instrumentation.
				_logger?.Debug()
					?.Log("No span to end for EndExecute event. Id: {Id}. The command was started outside of a transaction or is traced by a competing instrumentation.", id);
				return;
			}

			var isSqlException = (compositeState & 2) == 2;
			// 4 - is synchronous

			item.Span.Duration = (stop - item.Start) / (double)Stopwatch.Frequency * 1000;

			if (isSqlException)
			{
				item.Span.Outcome = Outcome.Failure;
				item.Span.CaptureError("Exception has occurred", sqlExceptionNumber != 0 ? $"SQL Exception {sqlExceptionNumber}" : null,
					null);
			}
			else
				item.Span.Outcome = Outcome.Success;

			item.Span.End();
		}
	}
}
