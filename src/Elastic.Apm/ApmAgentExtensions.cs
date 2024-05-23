// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.Logging;

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

			subscribers ??= [];
			var subscribersList = string.Join(", ", subscribers.Select(s => s.GetType().Name));

			agent.Logger.Trace()?.Log("Agent.Subscribe(), Agent Enabled: {AgentEnabled} Subscriber count: {NumberOfSubscribers}, ({Subscribers})",
				agent.Configuration.Enabled, subscribers.Length, subscribersList);

			if (!agent.Configuration.Enabled || subscribers.Length == 0)
				return disposable;

			foreach (var subscriber in subscribers)
				disposable.Add(subscriber.Subscribe(agent));

			if (agent is ApmAgent apmAgent)
				apmAgent.Disposables.Add(disposable);

			return disposable;
		}

		/// <summary> Used by integrations to register all known subscribers that ship as part of Elastic.Apm and the integration itself</summary>
		internal static IDisposable SubscribeIncludingAllDefaults(this IApmAgent agent, params IDiagnosticsSubscriber[] subscribers)
		{
			var defaultSubscribers = new IDiagnosticsSubscriber[]
			{
				new SqlClientDiagnosticSubscriber(),
				new HttpDiagnosticsSubscriber()
			};
			// on the off chance that someone manually references old nuget packages and injects them manually
			// make sure we reject them and favor the builtin subscribers instead.
			var rejectOldSubscribers = new[]
			{
				"Elastic.Apm.SqlClient.SqlClientDiagnosticSubscriber"
			};

			var userProvidedAndDefaultSubs = (subscribers ?? Array.Empty<IDiagnosticsSubscriber>())
				.Concat(defaultSubscribers)
				.GroupBy(s => s.GetType().FullName)
				.Where(g => !rejectOldSubscribers.Contains(g.Key))
				.Select(g => g.First())
				.ToArray();

			return agent.Subscribe(userProvidedAndDefaultSubs);
		}

		internal static IExecutionSegment GetCurrentExecutionSegment(this IApmAgent agent) =>
			agent.Tracer.CurrentSpan ?? (IExecutionSegment)agent.Tracer.CurrentTransaction;
	}

	internal class EmptyDisposable : IDisposable
	{
		private EmptyDisposable() { }

		public static EmptyDisposable Instance = new EmptyDisposable();

		public void Dispose() { }
	}

	/// <summary>
	/// A collection of <see cref="IDisposable"/> instances
	/// </summary>
	internal class CompositeDisposable : IDisposable
	{
		private readonly List<IDisposable> _disposables = new();
		private readonly object _lock = new();

		private bool _isDisposed;

		/// <summary>
		/// Adds an instance of <see cref="IDisposable"/> to the collection
		/// </summary>
		/// <param name="disposable">A disposable</param>
		/// <returns>This instance of <see cref="CompositeDisposable"/></returns>
		public CompositeDisposable Add(IDisposable disposable)
		{
			if (_isDisposed)
				throw new ObjectDisposedException(nameof(CompositeDisposable));

			lock (_lock)
			{
				if (_isDisposed)
					throw new ObjectDisposedException(nameof(CompositeDisposable));

				_disposables.Add(disposable);
				return this;
			}
		}

		public void Dispose()
		{
			if (_isDisposed)
				return;

			lock (_lock)
			{
				if (_isDisposed)
					return;

				_isDisposed = true;
				foreach (var d in _disposables)
					d?.Dispose();
			}
		}
	}
}
