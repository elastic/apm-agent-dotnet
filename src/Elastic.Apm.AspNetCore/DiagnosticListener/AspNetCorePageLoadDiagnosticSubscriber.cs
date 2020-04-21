using System;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	public class AspNetCorePageLoadDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			var subscriber = new DiagnosticInitializer(agent.Logger, new[] { new AspNetCorePageLoadDiagnosticListener(agent as ApmAgent), });
			retVal.Add(subscriber);

			retVal.Add(System.Diagnostics.DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
