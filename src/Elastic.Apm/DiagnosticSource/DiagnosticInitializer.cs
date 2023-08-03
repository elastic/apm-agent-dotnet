// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticSource
{
	internal class DiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable
	{
		private readonly IApmAgent _agent;
		private readonly IList<IDiagnosticListener> _listeners;
		private readonly ScopedLogger _logger;
		private readonly CompositeDisposable _subscriptions;

		internal DiagnosticInitializer(IApmAgent agent, IDiagnosticListener listener)
			: this(agent, new[] { listener }) { }

		internal DiagnosticInitializer(IApmAgent agent, IEnumerable<IDiagnosticListener> listeners)
		{
			_agent = agent ?? throw new ArgumentNullException(nameof(agent));
			_logger = agent.Logger.Scoped(nameof(DiagnosticInitializer));
			_listeners = listeners.Where(l => l != null).ToArray();
			_subscriptions = new CompositeDisposable();
		}

		public void OnCompleted() { }

		public void OnError(Exception error) { }

		public void OnNext(DiagnosticListener value)
		{
			var subscribedAny = false;

			foreach (var listener in _listeners)
			{
				if (value.Name != listener.Name)
					continue;

				subscribedAny = true;
				var listenerType = listener.GetType();
				if (_agent.SubscribedListeners.Add(listenerType) || listener.AllowDuplicates)
				{
					var subscription = value.Subscribe(listener);
					_subscriptions.Add(new Subscription(subscription, _agent, listenerType));
					_logger.Debug()
						?.Log("`{DiagnosticListenerName,20}' subscribed by: {DiagnosticListenerType, 20}", value.Name, listenerType.Name);
				}
				else
				{
					_logger.Debug()
						?.Log("`{DiagnosticListenerName,20}` already subscribed by {DiagnosticListenerType,20}", value.Name, listenerType.Name);
				}
			}

			if (!subscribedAny)
			{
				_logger.Trace()
					?.Log("`{DiagnosticListenerName,20}' not matched by any of: ({DiagnosticListeners})",
						value.Name,
						string.Join(", ", _listeners.Select(listener => listener.GetType().Name))
					);
			}
		}


		public void Dispose() => _subscriptions?.Dispose();


		/// <summary> Disposes the disposable and removes the type from subscribed listeners</summary>
		private class Subscription : IDisposable
		{
			private readonly IDisposable _disposable;
			private readonly IApmAgent _agent;
			private readonly Type _listenerType;

			public Subscription(IDisposable subscription, IApmAgent agent, Type listenerType)
			{
				_disposable = subscription;
				_agent = agent;
				_listenerType = listenerType;
			}

			public void Dispose()
			{
				_disposable.Dispose();
				_agent.SubscribedListeners.Remove(_listenerType);
			}
		}
	}
}
