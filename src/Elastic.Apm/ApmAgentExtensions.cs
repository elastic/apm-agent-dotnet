// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm
{
	public static class ApmAgentExtensions
	{
		/// <summary>
		/// Sets up multiple <see cref="IDiagnosticsSubscriber" />s to start listening to one or more
		/// <see cref="IDiagnosticListener" />s.
		/// <para />
		/// If the agent is not enabled, subscribers are not subscribed.
		/// </summary>
		/// <param name="agent">The agent to report diagnostics over</param>
		/// <param name="subscribers">
		/// An array of <see cref="IDiagnosticsSubscriber" /> that will set up
		/// <see cref="IDiagnosticListener" /> subscriptions
		/// </param>
		/// <returns>
		/// A disposable referencing all the subscriptions. Disposing this is not necessary for clean up, only to unsubscribe if
		/// desired.
		/// </returns>
		public static IDisposable Subscribe(this IApmAgent agent, params IDiagnosticsSubscriber[] subscribers)
		{
			var disposable = new CompositeDisposable();
			if (!agent.ConfigurationReader.Enabled || subscribers is null || subscribers.Length == 0)
				return disposable;

			foreach (var subscriber in subscribers)
				disposable.Add(subscriber.Subscribe(agent));

			if (agent is ApmAgent apmAgent)
				apmAgent.Disposables.Add(disposable);

			return disposable;
		}

		internal static IExecutionSegment GetCurrentExecutionSegment(this IApmAgent agent) =>
			agent.Tracer.CurrentSpan ?? (IExecutionSegment)agent.Tracer.CurrentTransaction;

	}

	/// <summary>
	/// A collection of <see cref="IDisposable"/> instances
	/// </summary>
	internal class CompositeDisposable : IDisposable
	{
		private readonly List<IDisposable> _disposables = new List<IDisposable>();
		private readonly object _lock = new object();

		private bool _isDisposed;

		/// <summary>
		/// Adds an instance of <see cref="IDisposable"/> to the collection
		/// </summary>
		/// <param name="disposable">A disposable</param>
		/// <returns>This instance of <see cref="CompositeDisposable"/></returns>
		public CompositeDisposable Add(IDisposable disposable)
		{
			if (_isDisposed) throw new ObjectDisposedException(nameof(CompositeDisposable));

			_disposables.Add(disposable);
			return this;
		}

		public void Dispose()
		{
			if (_isDisposed) return;

			lock (_lock)
			{
				if (_isDisposed) return;

				_isDisposed = true;
				foreach (var d in _disposables) d?.Dispose();
			}
		}
	}
}
