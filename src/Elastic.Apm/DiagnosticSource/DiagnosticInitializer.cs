using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elastic.Apm.DiagnosticSource
{
	internal class DiagnosticInitializer : IObserver<DiagnosticListener>
	{
		private readonly IEnumerable<IDiagnosticListener> _listeners;

		public DiagnosticInitializer(IEnumerable<IDiagnosticListener> listeners) => _listeners = listeners;

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(DiagnosticListener value)
		{
			foreach (var listener in _listeners)
			{
				if (value.Name == listener.Name) value.Subscribe(listener);
			}
		}
	}
}
