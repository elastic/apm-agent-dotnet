using System;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;

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
			agent.Logger.Debug()?.Log($"{nameof(AspNetCoreDiagnosticSubscriber)} starting to subscribe");

			var retVal = new CompositeDisposable();
			if (!agent.Configuration.Enabled)
				return retVal;

			var subscriber = new DiagnosticInitializer(agent, new AspNetCoreDiagnosticListener(agent as ApmAgent));
			retVal.Add(subscriber);

			retVal.Add(System.Diagnostics.DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			agent.Logger.Debug()?.Log($"{nameof(AspNetCoreDiagnosticSubscriber)} subscribed");

			return retVal;
		}
	}
}
