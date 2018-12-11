using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;
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
                                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.FFFZ"),
                                Context = transaction.Context
                            }
                        },
                    Service = transaction.service,
                };

                if(exception != null)
                {
                    var frames = new System.Diagnostics.StackTrace(exception).GetFrames();
                    error.Errors[0].Exception.Stacktrace = StacktraceHelper.GenerateApmStackTrace(frames, logger, "failed ASP.NET Core request");
                }

                Agent.PayloadSender.QueueError(error);
            }
        }
    }
}
