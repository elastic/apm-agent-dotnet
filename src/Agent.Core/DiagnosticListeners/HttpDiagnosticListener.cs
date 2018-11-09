using System;
using System.Collections.Generic;
using Elastic.Agent.Core.DiagnosticSource;

namespace Elastic.Agent.Core.DiagnosticListeners
{
    public class HttpDiagnosticListener : IDiagnosticListener
    {
        public string Name => "HttpHandlerDiagnosticListener";

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(KeyValuePair<string, object> value)
        {

        }
    }
}
