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
		private readonly ApmAgent _agent;
		private readonly IEnumerable<IDiagnosticListener> _listeners;
		private readonly IDiagnosticListener _listener;
		private readonly ScopedLogger _logger;
		private IDisposable _sourceSubscription;

		internal DiagnosticInitializer(IApmAgent agent, IEnumerable<IDiagnosticListener> listeners)
		{
			_agent = agent as ApmAgent;
			_logger = agent.Logger.Scoped(nameof(DiagnosticInitializer));
			_listeners = listeners;
		}

		internal DiagnosticInitializer(IApmAgent agent, IDiagnosticListener listener)
		{
			_agent = agent as ApmAgent;
			_logger = agent.Logger.Scoped(nameof(DiagnosticInitializer));
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
					if (_agent is null || _agent.SubscribedListeners.Add(_listener.GetType()))
					{
						_sourceSubscription = value.Subscribe(_listener);
						_logger.Debug()
							?.Log("Subscribed {DiagnosticListenerType} to `{DiagnosticListenerName}' events source",
								_listener.GetType().FullName, value.Name);
						subscribedAny = true;
					}
					else
					{
						_logger.Debug()?.Log("{DiagnosticListenerType} already subscribed to `{DiagnosticListenerName}' events source",
							_listener.GetType().FullName, value.Name);
					}
				}
			}
			else
			{
				foreach (var listener in _listeners)
				{
					if (value.Name == listener.Name)
					{
						if (_agent is null || _agent.SubscribedListeners.Add(listener.GetType()))
						{
							_sourceSubscription ??= new CompositeDisposable();
							((CompositeDisposable)_sourceSubscription).Add(value.Subscribe(listener));
							_logger.Debug()
								?.Log("Subscribed {DiagnosticListenerType} to `{DiagnosticListenerName}' events source",
									listener.GetType().FullName, value.Name);
							subscribedAny = true;
						}
						else
						{
							_logger.Debug()?.Log("{DiagnosticListenerType} already subscribed to `{DiagnosticListenerName}' events source",
								listener.GetType().FullName, value.Name);
						}
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
