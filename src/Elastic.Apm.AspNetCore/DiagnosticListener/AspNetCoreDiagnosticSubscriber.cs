using System;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	/// <summary>
	/// A Diagnostic listener to create transactions based on diagnostic source events for ASP.NET Core.
	/// This itself manages all transaction and error capturing without the need for a middleware.
	/// </summary>
	public class AspNetCoreDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			if (!agent.ConfigurationReader.Enabled)
				return retVal;

			var subscriber = new DiagnosticInitializer(agent.Logger, new AspNetCoreDiagnosticListener(agent as ApmAgent));
			retVal.Add(subscriber);

			retVal.Add(System.Diagnostics.DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
