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
				case string k when k == SmartSqlDiagnosticListenerExtensions.SMART_SQL_BEFORE_COMMAND_EXECUTER_EXECUTE:
					if (kv.Value is CommandExecuterExecuteBeforeEventData executerExecuteBeforeEventData)
					{
						var newsSpan = Agent.TransactionContainer.Transactions.Value.StartSpanInternal(executerExecuteBeforeEventData.Operation,
							ApiConstants.TypeDb);
						newsSpan.Tags.Add("statement",executerExecuteBeforeEventData.ExecutionContext.Request.RealSql);
						_spans.TryAdd(executerExecuteBeforeEventData.OperationId, newsSpan);
					}
					break;

				case string k when k==SmartSqlDiagnosticListenerExtensions.SMART_SQL_AFTER_COMMAND_EXECUTER_EXECUTE:
					if (kv.Value is CommandExecuterExecuteAfterEventData commandExecutedEventData)
					{
						if (_spans.TryRemove(commandExecutedEventData.OperationId,out var span))
						{
							span.Context.Db = new Database
							{
								Statement = commandExecutedEventData.ExecutionContext.Request.RealSql,
								Instance = commandExecutedEventData.ExecutionContext.Request.DataSourceChoice.ToString(),
								Type = Database.TypeSql
							};
							span.Tags.Add("fromcache", commandExecutedEventData.ExecutionContext.Result.FromCache.ToString());
							var dbConnection = commandExecutedEventData.ExecutionContext.DbSession.Connection;
							if (dbConnection == null)
							{
								return;
							}
							if (dbConnection.DataSource != null)
							{
								span.Tags.Add("Peer",dbConnection.DataSource);
							}
							if (dbConnection.Database != null)
							{
								span.Tags.Add("instance", dbConnection.Database);
							}

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
