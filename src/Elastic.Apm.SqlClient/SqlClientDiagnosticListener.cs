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
		private readonly IApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<Guid, ISpan> _processingQueries = new ConcurrentDictionary<Guid, ISpan>();

		private readonly PropertyFetcher _correlationIdFetcher = new PropertyFetcher("OperationId");
		private readonly PropertyFetcher _statisticsFetcher = new PropertyFetcher("Statistics");
		private readonly PropertyFetcher _commandPropertyFetcher = new PropertyFetcher("Command");

		private readonly PropertyFetcher _commandTextPropertyFetcher;
		private readonly PropertyFetcher _commandTypePropertyFetcher;
		private readonly PropertyFetcher _databasePropertyFetcher;

		private readonly PropertyFetcher _exceptionFetcher = new PropertyFetcher("Exception");

		public SqlClientDiagnosticListener(IApmAgent apmAgent)
		{
			_apmAgent = apmAgent;
			_logger = _apmAgent.Logger.Scoped(nameof(SqlClientDiagnosticListener));

			var connectionPropertyFetcher = new CascadePropertyFetcher(_commandPropertyFetcher, "Connection");
			_commandTextPropertyFetcher = new CascadePropertyFetcher(_commandPropertyFetcher, "CommandText");
			_commandTypePropertyFetcher = new CascadePropertyFetcher(_commandPropertyFetcher, "CommandType");

			_databasePropertyFetcher = new CascadePropertyFetcher(connectionPropertyFetcher, "Database");
		}

		public string Name => "SqlClientDiagnosticListener";

		// prefix - Microsoft.Data.SqlClient. or System.Data.SqlClient.
		public void OnNext(KeyValuePair<string, object> value)
		{
			if (value.Key.StartsWith("Microsoft.Data.SqlClient.") || value.Key.StartsWith("System.Data.SqlClient."))
			{
				switch (value.Key)
				{
					case { } s when s.EndsWith("WriteCommandBefore") && _apmAgent.Tracer.CurrentTransaction != null:
						HandleStartCommand(value.Value);
						break;
					case { } s when s.EndsWith("WriteCommandAfter"):
						HandleStopCommand(value.Value);
						break;
					case { } s when s.EndsWith("WriteCommandError"):
						HandleErrorCommand(value.Value);
						break;
				}
			}
		}

		private void HandleStartCommand(object payloadData)
		{
			try
			{
				var transaction = _apmAgent.Tracer.CurrentTransaction;
				var currentExecutionSegment = _apmAgent.Tracer.CurrentSpan ?? (IExecutionSegment)transaction;

				if (_correlationIdFetcher.Fetch(payloadData) is Guid operationId)
				{
					var commandText = _commandTextPropertyFetcher.Fetch(payloadData).ToString();
					var commandType =_commandTypePropertyFetcher.Fetch(payloadData).ToString();
					var instance = _databasePropertyFetcher.Fetch(payloadData).ToString();

					var span = currentExecutionSegment.StartSpan(
						commandText,
						ApiConstants.TypeDb,
						ApiConstants.SubtypeMssql);

					if (!_processingQueries.TryAdd(operationId, span)) return;

					span.Action = commandType switch
					{
						"Text" => ApiConstants.ActionQuery,
						"StoredProcedure" => ApiConstants.ActionExec,
						"TableDirect" => "tabledirect",
						_ => commandType
					};

					span.Context.Db = new Database
					{
						Statement = commandText,
						Instance = instance,
						Type = Database.TypeSql
					};
				}
			}
			catch (Exception ex)
			{
				//ignore
				_logger.Error()?.LogException(ex, "Exception was thrown while handling 'command started event'");
			}
		}

		private void HandleStopCommand(object payloadData)
		{
			try
			{
				if (_correlationIdFetcher.Fetch(payloadData) is Guid operationId)
				{
					if (!_processingQueries.TryRemove(operationId, out var span)) return;

					if (_statisticsFetcher.Fetch(payloadData) is IDictionary<object, object> statistics &&
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

		private void HandleErrorCommand(object payloadData)
		{
			try
			{
				if (_correlationIdFetcher.Fetch(payloadData) is Guid operationId)
				{
					if (!_processingQueries.TryRemove(operationId, out var span)) return;

					if (_exceptionFetcher.Fetch(payloadData) is Exception exception) span.CaptureException(exception);

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
