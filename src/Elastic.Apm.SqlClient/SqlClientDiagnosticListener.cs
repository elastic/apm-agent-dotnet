using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.SqlClient
{
	public class SqlClientDiagnosticListener : IDiagnosticListener
	{
		private class PropertyFetcherSet
		{
			public PropertyFetcher StartCorrelationId { get; } = new PropertyFetcher("OperationId");
			public PropertyFetcher StopCorrelationId { get; } = new PropertyFetcher("OperationId");
			public PropertyFetcher ErrorCorrelationId { get; } = new PropertyFetcher("OperationId");

			public PropertyFetcher Statistics { get; } = new PropertyFetcher("Statistics");

			public PropertyFetcher CommandText { get; }
			public PropertyFetcher CommandType { get; }
			public PropertyFetcher Database { get; }

			public PropertyFetcher Exception { get; } = new PropertyFetcher("Exception");

			public PropertyFetcherSet()
			{
				var commandPropertyFetcher = new PropertyFetcher("Command");
				var connectionPropertyFetcher = new CascadePropertyFetcher(commandPropertyFetcher, "Connection");

				CommandText = new CascadePropertyFetcher(commandPropertyFetcher, "CommandText");
				CommandType = new CascadePropertyFetcher(commandPropertyFetcher, "CommandType");

				Database = new CascadePropertyFetcher(connectionPropertyFetcher, "Database");
			}
		}

		private readonly IApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<Guid, ISpan> _processingQueries = new ConcurrentDictionary<Guid, ISpan>();

		private readonly PropertyFetcherSet _systemPropertyFetcherSet = new PropertyFetcherSet();
		private readonly PropertyFetcherSet _microsoftPropertyFetcherSet = new PropertyFetcherSet();


		public SqlClientDiagnosticListener(IApmAgent apmAgent)
		{
			_apmAgent = apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(SqlClientDiagnosticListener));
		}

		public string Name => "SqlClientDiagnosticListener";

		// prefix - Microsoft.Data.SqlClient. or System.Data.SqlClient.
		public void OnNext(KeyValuePair<string, object> value)
		{
			if (value.Key.StartsWith("Microsoft.Data.SqlClient.") || value.Key.StartsWith("System.Data.SqlClient."))
			{
				switch (value.Key)
				{
					case string s when s.EndsWith("WriteCommandBefore") && _apmAgent.Tracer.CurrentTransaction != null:
						HandleStartCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
						break;
					case string s when s.EndsWith("WriteCommandAfter"):
						HandleStopCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
						break;
					case string s when s.EndsWith("WriteCommandError"):
						HandleErrorCommand(value.Value, value.Key.StartsWith("System") ? _systemPropertyFetcherSet : _microsoftPropertyFetcherSet);
						break;
				}
			}
		}

		private void HandleStartCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				var transaction = _apmAgent.Tracer.CurrentTransaction;
				var currentExecutionSegment = _apmAgent.Tracer.CurrentSpan ?? (IExecutionSegment)transaction;

				if (propertyFetcherSet.StartCorrelationId.Fetch(payloadData) is Guid operationId)
				{
					var commandText = propertyFetcherSet.CommandText.Fetch(payloadData).ToString();
					var commandType = propertyFetcherSet.CommandType.Fetch(payloadData).ToString();
					var instance = propertyFetcherSet.Database.Fetch(payloadData).ToString();

					var span = currentExecutionSegment.StartSpan(
						commandText,
						ApiConstants.TypeDb,
						ApiConstants.SubtypeMssql);

					if (!_processingQueries.TryAdd(operationId, span)) return;

					switch (commandType)
					{
						case "Text":
							span.Action = ApiConstants.ActionQuery;
							break;
						case "StoredProcedure":
							span.Action = ApiConstants.ActionExec;
							break;
						case "TableDirect":
							span.Action = "tabledirect";
							break;
						default:
							span.Action = commandType;
							break;
					}

					span.Context.Db = new Database { Statement = commandText, Instance = instance, Type = Database.TypeSql };
				}
			}
			catch (Exception ex)
			{
				//ignore
				_logger.Error()?.LogException(ex, "Exception was thrown while handling 'command started event'");
			}
		}

		private void HandleStopCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.StopCorrelationId.Fetch(payloadData) is Guid operationId)
				{
					if (!_processingQueries.TryRemove(operationId, out var span)) return;

					if (propertyFetcherSet.Statistics.Fetch(payloadData) is IDictionary<object, object> statistics &&
						statistics.ContainsKey("ExecutionTime") && statistics["ExecutionTime"] is long duration)
						span.Duration = duration;

					span.End();
				}
			}
			catch (Exception ex)
			{
				// ignore
				_logger.Error()?.LogException(ex, "Exception was thrown while handling 'command succeeded event'");
			}
		}

		private void HandleErrorCommand(object payloadData, PropertyFetcherSet propertyFetcherSet)
		{
			try
			{
				if (propertyFetcherSet.ErrorCorrelationId.Fetch(payloadData) is Guid operationId)
				{
					if (!_processingQueries.TryRemove(operationId, out var span)) return;

					if (propertyFetcherSet.Exception.Fetch(payloadData) is Exception exception) span.CaptureException(exception);

					span.End();
				}
			}
			catch (Exception ex)
			{
				// ignore
				_logger.Error()?.LogException(ex, "Exception was thrown while handling 'command failed event'");
			}
		}

		public void OnError(Exception error)
		{
			// do nothing because it's not necessary to handle such event from provider
		}

		public void OnCompleted()
		{
			// do nothing because it's not necessary to handle such event from provider
		}
	}
}
