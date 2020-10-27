// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
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
			var retVal = new CompositeDisposable();

			if (!agent.ConfigurationReader.Enabled)
				return retVal;

			var logger = agent.Logger.Scoped(nameof(HttpDiagnosticsSubscriber));


			var initializer = new DiagnosticInitializer(agent.Logger, new[] { HttpDiagnosticListener.New(agent) });
			retVal.Add(initializer);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(initializer));

			return retVal;
		}
	}
}
