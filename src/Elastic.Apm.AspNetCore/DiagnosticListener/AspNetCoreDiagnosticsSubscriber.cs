using System;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	/// <summary>
	/// Activates the <see cref="AspNetCoreDiagnosticListener" /> which enables
	/// capturing errors within an ASP.NET Core application.
	/// </summary>
	public class AspNetCoreDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Start listening for ASP.NET Core related diagnostic source events.
		/// </summary>
		public IDisposable Subscribe(IApmAgent agent)
		{
			var retVal = new CompositeDisposable();
			var subscriber = new DiagnosticInitializer(agent.Logger, new[] { new AspNetCoreDiagnosticListener(agent) });
			retVal.Add(subscriber);

			retVal.Add(System.Diagnostics.DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
