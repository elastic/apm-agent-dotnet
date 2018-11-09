using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Elastic.Agent.Core.DiagnosticSource;
using Elastic.Agent.Core.Model.Payload;

namespace Elastic.Agent.Core.DiagnosticListeners
{
    public class HttpDiagnosticListener : IDiagnosticListener
    {
        public string Name => "HttpHandlerDiagnosticListener";

        //TODO: find better way to keep track of respones
        private readonly ConcurrentDictionary<HttpRequestMessage, DateTime> _startedRequests = new ConcurrentDictionary<HttpRequestMessage, DateTime>();

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(KeyValuePair<string, object> kv)
        {
            switch (kv.Key)
            {
                case "System.Net.Http.HttpRequestOut.Start": //TODO: look for consts 
                    if(kv.Value is HttpRequestMessage requestMessage)
                    {
                        _startedRequests.TryAdd(requestMessage, DateTime.UtcNow);
                    }
                    break;

                case "System.Net.Http.HttpRequestOut.Stop":
                    var request = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Request").GetValue(kv.Value) as HttpRequestMessage;
                    var response = kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("Response").GetValue(kv.Value) as HttpResponseMessage;
                    var requestTaskStatus = (TaskStatus)kv.Value.GetType().GetTypeInfo().GetDeclaredProperty("RequestTaskStatus").GetValue(kv.Value);

                    var span = new Span
                    {
                        Name = "Http request",
                        Type = request.Method.Method,
                        Context = new Span.ContextC
                        {
                            Http = new Http
                            {
                                Url = request.RequestUri.ToString() //TODO: don't we repost response code, and other things? Intake
                            }
                        }
                    };
              
                    if (_startedRequests.TryRemove(request, out DateTime requestStart))
                    {
                        var requestDuration = DateTime.UtcNow - requestStart;
                        span.Duration = (int)requestDuration.TotalMilliseconds; //TODO: don't cast!
                    }

                    TransactionContainer.Transactions[0].Spans.Add(span);
                    break;
                default:
                    break;
            }
        }
    }
}
