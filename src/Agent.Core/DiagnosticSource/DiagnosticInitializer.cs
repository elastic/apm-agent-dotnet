using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elastic.Agent.Core.DiagnosticSource
{
    public class DiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly IEnumerable<IDiagnosticListener> _listeners;

        public DiagnosticInitializer(IEnumerable<IDiagnosticListener> listeners)
        {
            _listeners = listeners;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(DiagnosticListener value)
        {
            foreach (var listener in _listeners)
            {
                if (value.Name == listener.Name)
                {
                    value.Subscribe(listener);
                }
            }
        }
    }
}
