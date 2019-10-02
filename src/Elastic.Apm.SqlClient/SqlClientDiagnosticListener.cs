using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;

namespace Elastic.Apm.SqlClient
{
	public class SqlClientDiagnosticListener : IDiagnosticListener
	{
		private readonly IApmAgent _apmAgent;
		private readonly IApmLogger _logger;

		private readonly ConcurrentDictionary<Guid, ISpan> _processingQueries = new ConcurrentDictionary<Guid, ISpan>();

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

				if (payloadData.GetProperty("OperationId") is Guid operationId)
				{
					var providerType = payloadData.GetProperty("Command").GetProperty("Connection").GetType().FullName;
					var commandText = payloadData.GetProperty("Command").GetProperty("CommandText").ToString();
					var commandType = payloadData.GetProperty("Command").GetProperty("CommandType").ToString();
					var instance = payloadData.GetProperty("Command")
						.GetProperty("Connection")
						.GetProperty("Database")
						.ToString();

					var subType = providerType switch
					{
						{ } s when s.Contains("Sqlite") => ApiConstants.SubtypeSqLite,
						{ } s when s.Contains("SqlConnection") => ApiConstants.SubtypeMssql,
						_ => providerType
					};

					var span = currentExecutionSegment.StartSpan(
						commandText,
						ApiConstants.TypeDb,
						subType);

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
				if (payloadData.GetProperty("OperationId") is Guid operationId)
				{
					if (!_processingQueries.TryRemove(operationId, out var span)) return;

					if (payloadData.GetProperty("Statistics") is IDictionary<object, object> statistics &&
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
				if (payloadData.GetProperty("OperationId") is Guid operationId)
				{
					if (!_processingQueries.TryRemove(operationId, out var span)) return;

					if (payloadData.GetProperty("Exception") is Exception exception) span.CaptureException(exception);

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

	public static class PropertyExtensions
	{
		public static object GetProperty(this object _this, string propertyName)
		{
			return _this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName)?.GetValue(_this);
		}
	}
}
