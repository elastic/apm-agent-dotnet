using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elastic.Apm.DiagnosticSource
{
	internal class DiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
	{
		private readonly IEnumerable<IDiagnosticListener> _listeners;

		internal DiagnosticInitializer(IEnumerable<IDiagnosticListener> listeners) => _listeners = listeners;

		private IDisposable _sourceSubscription;

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(DiagnosticListener value)
		{
			foreach (var listener in _listeners)
				if (value.Name == listener.Name)
					_sourceSubscription = value.Subscribe(listener);
		}

		public void Dispose() => _sourceSubscription?.Dispose();
	}
}
