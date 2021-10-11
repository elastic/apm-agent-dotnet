// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticSource
{
	internal class DiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
	{
		private readonly IEnumerable<IDiagnosticListener> _listeners;
		private readonly IDiagnosticListener _listener;
		private readonly ScopedLogger _logger;
		private IDisposable _sourceSubscription;

		internal DiagnosticInitializer(IApmLogger baseLogger, IEnumerable<IDiagnosticListener> listeners)
		{
			_logger = baseLogger.Scoped(nameof(DiagnosticInitializer));
			_listeners = listeners;
		}

		internal DiagnosticInitializer(IApmLogger baseLogger, IDiagnosticListener listener)
		{
			_logger = baseLogger.Scoped(nameof(DiagnosticInitializer));
			_listener = listener;
		}

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(DiagnosticListener value)
		{
			var subscribedAny = false;

			if (_listener != null)
			{
				if (value.Name == _listener.Name)
				{
					_sourceSubscription = value.Subscribe(_listener);
					_logger.Debug()
						?.Log("Subscribed {DiagnosticListenerType} to `{DiagnosticListenerName}' events source",
							_listener.GetType().FullName, value.Name);
					subscribedAny = true;
				}
			}
			else
			{
				foreach (var listener in _listeners)
				{
					if (value.Name == listener.Name)
					{
						_sourceSubscription ??= new CompositeDisposable();
						((CompositeDisposable)_sourceSubscription).Add(value.Subscribe(listener));
						_logger.Debug()
							?.Log("Subscribed {DiagnosticListenerType} to `{DiagnosticListenerName}' events source",
								listener.GetType().FullName, value.Name);
						subscribedAny = true;
					}
				}
			}

			if (!subscribedAny)
			{
				_logger.Trace()
					?.Log(
						"There are no listeners in the current batch ({DiagnosticListeners}) that would like to subscribe to `{DiagnosticListenerName}' events source",
						_listener != null
							? _listener.GetType().FullName
							: string.Join(", ", _listeners.Select(listener => listener.GetType().FullName)),
						value.Name);
			}
		}

		public void Dispose() => _sourceSubscription?.Dispose();
	}
}
