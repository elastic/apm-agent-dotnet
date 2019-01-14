using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm
{
	public static class ApmAgentExtensions
	{
		/// <summary>
		/// Sets up multiple <see cref="IDiagnosticsSubscriber" />'s to start listening to one or more
		/// <see cref="IDiagnosticListener" />'s
		/// </summary>
		/// <param name="agent">The agent to report diagnostics over</param>
		/// <param name="subscribers">
		/// An array of <see cref="IDiagnosticSubscriber" /> that will set up
		/// <see cref="IDiagnosticListener" /> subscriptions
		/// </param>
		/// <returns>
		/// A disposable referencing all the subscriptions, disposing this is not necessary for clean up only to
		/// unsubscribe if desired.
		/// </returns>
		public static IDisposable Subscribe(this IApmAgent agent, params IDiagnosticsSubscriber[] subscribers)
		{
			var subs = subscribers ?? Array.Empty<IDiagnosticsSubscriber>();
			return subs.Aggregate(new CompositeDisposable(), (d, s) => d.Add(s.Subscribe(agent)));
		}
	}

	internal class CompositeDisposable : IDisposable
	{
		private readonly object _lock = new object();

		private bool _isDisposed;

		private List<IDisposable> _disposables = new List<IDisposable>();

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
				foreach (var d in _disposables) d.Dispose();
			}
		}
	}
}
