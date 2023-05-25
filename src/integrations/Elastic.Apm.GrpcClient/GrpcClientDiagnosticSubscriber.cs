// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.GrpcClient
{
	public class GrpcClientDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		internal GrpcClientDiagnosticListener Listener { get; private set; }

		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();

			if (!agent.ConfigurationReader.Enabled)
				return retVal;

			Listener = new GrpcClientDiagnosticListener(agent as ApmAgent);
			var subscriber = new DiagnosticInitializer(agent, Listener);
			retVal.Add(subscriber);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
