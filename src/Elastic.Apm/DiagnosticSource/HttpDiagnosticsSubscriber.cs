// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticListeners;

namespace Elastic.Apm.DiagnosticSource
{
	/// <summary>
	/// Activates the <see cref="HttpDiagnosticListener" /> which enables
	/// capturing outgoing web requests created by <see cref="System.Net.Http.HttpClient" />.
	/// </summary>
	public class HttpDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		private readonly bool _startHttpSpan;

		public HttpDiagnosticsSubscriber() : this(true)
		{
		}

		internal HttpDiagnosticsSubscriber(bool startHttpSpan) => _startHttpSpan = startHttpSpan;

		/// <summary>
		/// Start listening for HttpClient diagnostic source events.
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			var realAgent = agent as ApmAgent;

			if (realAgent != null && realAgent.HttpDiagnosticListener != null)
			{
				realAgent.HttpDiagnosticListener.StartHttpSpan = true;
				return retVal;
			}

			var diagnosticListener = HttpDiagnosticListener.New(agent, _startHttpSpan);
			var initializer = new DiagnosticInitializer(agent.Logger, new[] { diagnosticListener });
			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			if (realAgent != null)
				realAgent.HttpDiagnosticListener = diagnosticListener;

			return retVal;
		}
	}
}
