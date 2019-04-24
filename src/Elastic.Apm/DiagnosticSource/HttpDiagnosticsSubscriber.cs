﻿using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DiagnosticSource
{
	/// <summary>
	/// Activates the <see cref="HttpDiagnosticListener" /> which enables
	/// capturing outgoing web requests created by <see cref="System.Net.Http.HttpClient" />.
	/// </summary>
	public class HttpDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Start listening for HttpClient diagnostic source events.
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			var logger = agent.Logger.Scoped(nameof(HttpDiagnosticsSubscriber));

			var retVal = new CompositeDisposable();
			var initializer = new DiagnosticInitializer(agent.Logger, new[] { HttpDiagnosticListener.New(agent) });
			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			return retVal;
		}
	}
}
