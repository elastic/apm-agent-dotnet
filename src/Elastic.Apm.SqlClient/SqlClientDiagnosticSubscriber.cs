using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.SqlClient
{
	public class SqlClientDiagnosticSubscriber : IDiagnosticsSubscriber
	{
		public IDisposable Subscribe(IApmAgent agentComponents)
		{
			var retVal = new CompositeDisposable();
			if (PlatformDetection.IsDotNetCore)
			{
				var initializer = new DiagnosticInitializer(agentComponents.Logger, new[] { new SqlClientDiagnosticListener(agentComponents) });

				retVal.Add(initializer);

				retVal.Add(DiagnosticListener
					.AllListeners
					.Subscribe(initializer));
			}
			else
			{
				retVal.Add(new SqlEventListener(agentComponents));
			}

			return retVal;
		}
	}
}
