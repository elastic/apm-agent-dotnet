using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using SmartSql.Diagnostics;

namespace Elastic.Apm.SmartSql
{
	internal class SmartSqlDiagnosticListener : IDiagnosticListener
	{
		private readonly ConcurrentDictionary<Guid, Span> _spans = new ConcurrentDictionary<Guid, Span>();

		public SmartSqlDiagnosticListener(IApmAgent agent) => Logger = agent.Logger?.Scoped(nameof(SmartSqlDiagnosticListener));

		private ScopedLogger Logger { get; }

		public string Name => "SmartSqlDiagnosticListener";

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(KeyValuePair<string, object> kv)
		{
			switch (kv.Key)
			{
				case string k when k == SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_COMMAND_EXECUTER_EXECUTE
					&& Agent.TransactionContainer.Transactions.Value != null:
					if (kv.Value is CommandExecuterExecuteBeforeEventData executerExecuteBeforeEventData)
					{
						var newsSpan = Agent.TransactionContainer.Transactions.Value.StartSpanInternal(executerExecuteBeforeEventData.Operation,
							ApiConstants.TypeDb);
						_spans.TryAdd(executerExecuteBeforeEventData.OperationId, newsSpan);
					}
					break;

				case string k when k == SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_COMMAND_EXECUTER_EXECUTE
					&& Agent.TransactionContainer.Transactions.Value != null:
					if (kv.Value is CommandExecuterExecuteAfterEventData commandExecutedEventData)
					{
						if (_spans.TryRemove(commandExecutedEventData.OperationId, out var span))
						{
							span.Context.Db = new Database
							{
								Statement = commandExecutedEventData.ExecutionContext.Request.RealSql,
								Instance = commandExecutedEventData.ExecutionContext.Request.DataSourceChoice.ToString(),
								Type = Database.TypeSql
							};
							//indicate the result is from cache or db
							span.Tags.Add("ResultFromCache", commandExecutedEventData.ExecutionContext.Result.FromCache.ToString());
							span.Subtype = commandExecutedEventData.ExecutionContext.DbSession.Connection.GetType().FullName;
							switch (commandExecutedEventData.ExecutionContext.Request.CommandType)
							{
								case CommandType.Text:
									span.Action = ApiConstants.ActionQuery;
									break;
								case CommandType.StoredProcedure:
									span.Action = ApiConstants.ActionExec;
									break;
								case CommandType.TableDirect:
									span.Action = "tabledirect";
									break;
								default:
									span.Action = commandExecutedEventData.ExecutionContext.Request.CommandType.ToString();
									break;
							}
							span.End();
						}
					}

					break;
			}
		}
	}
}
