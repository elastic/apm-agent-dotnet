using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticSource
{
	internal class DiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
	{
		private readonly IEnumerable<IDiagnosticListener> _listeners;
		private readonly ScopedLogger _logger;

		internal DiagnosticInitializer(IApmLogger baseLogger, IEnumerable<IDiagnosticListener> listeners)
		{
			_logger = baseLogger.Scoped(nameof(DiagnosticInitializer));
			_listeners = listeners;
		}

		private IDisposable _sourceSubscription;

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(DiagnosticListener value)
		{
			var subscribedAny = false;
			foreach (var listener in _listeners)
			{
				if (value.Name == listener.Name)
				{
					_sourceSubscription = value.Subscribe(listener);
					_logger.Debug()
						?.Log("Subscribed {DiagnosticListenerType} to `{DiagnosticListenerName}' events source",
							listener.GetType().FullName, value.Name);
					subscribedAny = true;
				}
			}

			if (!subscribedAny)
				_logger.Trace()
					?.Log("There are no listeners that would like to subscribe to `{DiagnosticListenerName}' events source", value.Name);
		}

		public void Dispose() => _sourceSubscription?.Dispose();
	}
}
