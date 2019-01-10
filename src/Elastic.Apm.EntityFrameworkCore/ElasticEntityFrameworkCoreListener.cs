using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;

namespace Elastic.Apm.EntityFrameworkCore
{
	/// <summary>
	/// Manages the Entity Framework Core listener, which listenes to EF Core events
	/// </summary>
	public class ElasticEntityFrameworkCoreListener
	{
		/// <summary>
		/// Start listening for EF Core diagnosticsource events
		/// </summary>
		public void Start(AbstractLogger logger) => DiagnosticListener
			.AllListeners
			.Subscribe(new DiagnosticInitializer(new [] { new EfCoreDiagnosticListener(logger) }));
	}
}
