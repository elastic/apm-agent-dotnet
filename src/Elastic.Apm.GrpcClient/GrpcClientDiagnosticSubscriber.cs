using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.GrpcClient
{
	public class GrpcClientDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();

			if (!agent.ConfigurationReader.Enabled)
				return retVal;

			var subscriber = new DiagnosticInitializer(agent.Logger, new[] { new GrpcClientDiagnosticListener(agent as ApmAgent) });
			retVal.Add(subscriber);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
