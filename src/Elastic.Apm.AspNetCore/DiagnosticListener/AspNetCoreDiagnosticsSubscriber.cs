using System;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.AspNetCore.DiagnosticListener
{
	/// <summary>
	/// Activates the <see cref="AspNetCoreDiagnosticListener"/> which enables
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
			var listener = new AspNetCoreDiagnosticListener(agent);
			retVal.Add(listener);

			retVal.Add(System.Diagnostics.DiagnosticListener
				.AllListeners
				.Subscribe(new DiagnosticInitializer(new[] { listener })));

			return retVal;
		}
	}
}
