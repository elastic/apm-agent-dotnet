using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.EntityFrameworkCore
{
	/// <summary>
	/// Manages the Entity Framework Core listener, which listenes to EF Core events
	/// </summary>
	public class EfCoreDiagnosticsSubscriber : IDiagnosticsSubscriber
	{
		/// <summary>
		/// Start listening for EF Core diagnosticsource events
		/// </summary>
		public IDisposable Subscribe(IApmAgent agentComponents)
		{
			var retVal = new CompositeDisposable();
			var subscriber = new DiagnosticInitializer(new[] { new EfCoreDiagnosticListener(agentComponents) });
			retVal.Add(subscriber);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
