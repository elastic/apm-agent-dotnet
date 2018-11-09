using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Agent.Core;
using Elastic.Agent.Core.DiagnosticSource;
using Elastic.Agent.Core.Model.Payload;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Elastic.Agent.EntityFrameworkCore
{
    public class EfCoreDiagnosticListener : IDiagnosticListener
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
                    if (kv.Value is CommandEventData commandEventData)
                    {
                        var newSpan = new Span();

                        //TODO: should not parse -> change datatype of Timestamp
                        DateTime.TryParse(TransactionContainer.Transactions[0].Timestamp, out DateTime transactionStartTime);
                        newSpan.Start = (decimal)(DateTime.UtcNow - transactionStartTime).TotalMilliseconds;
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
                            span.Duration = (int)commandExecutedEventData.Duration.TotalMilliseconds; //TODO: don't cast!
                            span.Name = "EFCore Db";
                            span.Type = commandExecutedEventData.Command.CommandType.ToString();

                            TransactionContainer.Transactions[0].Spans.Add(span);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
