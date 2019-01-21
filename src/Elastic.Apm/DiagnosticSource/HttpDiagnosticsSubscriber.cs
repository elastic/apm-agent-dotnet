using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticListeners;

namespace Elastic.Apm.DiagnosticSource
{
	/// <summary>
	/// Activates the <see cref="HttpDiagnosticListener"/> which enables
	/// capturing outgoing web requests created by <see cref="System.Net.Http.HttpClient"/>.
	/// </summary>
	public class HttpDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		private HttpDiagnosticListener _listener;

		/// <summary>
		/// Start listening for HttpClient diagnostic source events.
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			_listener = new HttpDiagnosticListener(agent);
			retVal.Add(_listener);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(new DiagnosticInitializer(new[] { _listener })));

			return retVal;
		}
	}
}
