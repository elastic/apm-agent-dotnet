using System;
using System.Diagnostics;
using Elastic.Apm.DiagnosticSource;

namespace Elastic.Apm.SmartSql
{
	public class SmartSqlDiagnosticsSubscriber:IDiagnosticsSubscriber
	{
		public IDisposable Subscribe(IApmAgent agentComponents)
		{
			var retVal = new CompositeDisposable();
			var subscriber = new DiagnosticInitializer(agentComponents.Logger, new[] { new SmartSqlDiagnosticListener(agentComponents),  });
			retVal.Add(subscriber);

			retVal.Add(DiagnosticListener
				.AllListeners
				.Subscribe(subscriber));

			return retVal;
		}
	}
}
