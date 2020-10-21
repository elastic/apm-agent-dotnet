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
		private readonly ScopedLogger _logger;
		private readonly CompositeDisposable _sourceSubscription;

		internal DiagnosticInitializer(IApmLogger baseLogger, IEnumerable<IDiagnosticListener> listeners)
		{
			_logger = baseLogger.Scoped(nameof(DiagnosticInitializer));
			_listeners = listeners;
			_sourceSubscription = new CompositeDisposable();
		}

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(DiagnosticListener value)
		{
			var subscribedAny = false;
			foreach (var listener in _listeners)
			{
				if (value.Name == listener.Name)
				{
					_sourceSubscription.Add(value.Subscribe(listener));

					_logger.Debug()
						?.Log("Subscribed {DiagnosticListenerType} to `{DiagnosticListenerName}' events source",
							listener.GetType().FullName, value.Name);
					subscribedAny = true;
				}
			}

			if (!subscribedAny)
			{
				_logger.Trace()
					?.Log(
						"There are no listeners in the current batch ({DiagnosticListeners}) that would like to subscribe to `{DiagnosticListenerName}' events source",
						string.Join(", ", _listeners.Select(listener => listener.GetType().FullName)),
						value.Name);
			}
		}

		public void Dispose() => _sourceSubscription.Dispose();
	}
}
