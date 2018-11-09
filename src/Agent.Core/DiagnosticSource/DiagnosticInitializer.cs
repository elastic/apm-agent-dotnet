using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elastic.Agent.Core.DiagnosticSource
{
    public class DiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
    {
        private readonly IDiagnosticListener _listener; //TODO: make this a List, we'll have more from those

        public DiagnosticInitializer(IDiagnosticListener listener)
        {
            _listener = listener;
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
            if(value.Name == _listener.Name)
            {
                Console.WriteLine($"Registered {_listener.Name}");
                value.Subscribe(_listener);
            }
        }
    }
}
