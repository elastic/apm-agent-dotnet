using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Model.Payload;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elastic.Apm.EntityFrameworkCore
{
    internal class EfCoreDiagnosticListener : IDiagnosticListener
    {
        public string Name => "Microsoft.EntityFrameworkCore";

        private readonly ConcurrentDictionary<Guid, Span> _spans = new ConcurrentDictionary<Guid, Span>();

        public void OnCompleted() {}

        public void OnError(Exception error) {}

        public void OnNext(KeyValuePair<string, object> kv)
        {
            switch (kv.Key)
            {
                case string k when k == RelationalEventId.CommandExecuting.Name:
                    if (TransactionContainer.Transactions.Value != null && kv.Value is CommandEventData commandEventData)
                    {
                        var newSpan = TransactionContainer.Transactions.Value.StartSpan(
                                            commandEventData.Command.CommandText, Span.TYPE_DB);
                      
                        _spans.TryAdd(commandEventData.CommandId, newSpan);
                    }
                    break;
                case string k when k == RelationalEventId.CommandExecuted.Name:
                    if (kv.Value is CommandExecutedEventData commandExecutedEventData)
                    {
                        if (_spans.TryRemove(commandExecutedEventData.CommandId, out Span span))
                        {
                            span.Context = new Span.ContextC
                            {
                                Db = new Db
                                {
                                    Statement = commandExecutedEventData.Command.CommandText,
                                    Instance = commandExecutedEventData.Command.Connection.Database,
                                    Type = "sql"
                                }
                            };
                            span.Duration = commandExecutedEventData.Duration.TotalMilliseconds;

                            var providerType = commandExecutedEventData.Command.Connection.GetType().FullName;

                            switch (providerType)
                            {
                                case string str when str.Contains("Sqlite"):
                                    span.Subtype = Span.SUBTYPE_SQLITE;
                                    break;
                                case string str when str.Contains("SqlConnection"):
                                    span.Subtype = Span.SUBTYPE_MSSQL;
                                    break;
                                default:
                                    span.Subtype = providerType; //TODO, TBD: this is an unknown provider
                                    break;
                            }

                            switch (commandExecutedEventData.Command.CommandType)
                            { 
                                case System.Data.CommandType.Text:
                                    span.Action = Span.ACTION_QUERY;
                                    break;
                                case System.Data.CommandType.StoredProcedure:
                                    span.Action = Span.ACTION_EXEC;
                                    break;
                                case System.Data.CommandType.TableDirect:
                                    span.Action = "tabledirect";
                                    break;
                                default:
                                    //TODO log
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
