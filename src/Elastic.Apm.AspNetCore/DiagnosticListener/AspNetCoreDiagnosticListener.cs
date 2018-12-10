using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Microsoft.AspNetCore.Http;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
    public class AspNetCoreDiagnosticListener : IDiagnosticListener
    {
        public string Name => "Microsoft.AspNetCore";

        private readonly AbstractLogger logger;

        public AspNetCoreDiagnosticListener()
            => logger = Agent.CreateLogger(Name);

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(KeyValuePair<string, object> kv)
        {
            if (kv.Key == "Microsoft.AspNetCore.Diagnostics.UnhandledException" || kv.Key == "Microsoft.AspNetCore.Diagnostics.HandledException")
            {
                var context = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("httpContext").GetValue(kv.Value) as HttpContext;
                var exception = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("exception").GetValue(kv.Value) as Exception;

                var transaction = TransactionContainer.Transactions?.Value[0];
                if (transaction == null)
                {
                    return;
                }
                var error = new Error
                {
                    Errors = new List<Error.Err>
                        {
                            new Error.Err
                            {
                                Culprit = "ASP.NET Core Unhandled Exception",
                                Id = Guid.NewGuid(),
                                Transaction = new Error.Err.Trans
                                {
                                    Id = transaction.Id
                                },
                                Exception = new CapturedException
                                {
                                    Message = exception.Message,
                                    Type = exception.GetType().FullName,
                                    Handled = kv.Key  == "Microsoft.AspNetCore.Diagnostics.HandledException"
                                },
                                TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"),
                                Context = transaction.Context
                            }
                        },
                    Service = transaction.service,
                };

                if(exception != null)
                {
                    var frames = new System.Diagnostics.StackTrace(exception).GetFrames();
                    var stackFrames = new List<Stacktrace>(frames.Length);
                    try
                    {
                        foreach (var item in frames)
                        {
                            var fileName = item?.GetMethod()?.DeclaringType?.Assembly?.GetName()?.Name;
                            if (String.IsNullOrEmpty(fileName))
                            {
                                continue; //since filename is required by the server, if we don't have it we skip the frame
                            }

                            stackFrames.Add(new Stacktrace
                            {
                                Function = item?.GetMethod()?.Name,
                                Filename = fileName,
                                Module = item?.GetMethod()?.ReflectedType?.Name
                            });
                        }

                        error.Errors[0].Exception.Stacktrace = stackFrames;
                    }
                    catch (Exception e)
                    {
                        logger.LogWarning($"Failed capturing stacktrace Error {error.Errors[0].Id}");
                        logger.LogDebug($"{e.GetType().Name}: {e.Message}");
                    }
                }

                Agent.PayloadSender.QueueError(error);
            }
        }
    }
}
