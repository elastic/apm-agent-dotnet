using System;
using Elastic.Apm;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.SqlClient;

internal class StartupHook
{
	public static void Initialize()
	{

		Agent.Setup(new AgentComponents());
		Agent.Subscribe(new HttpDiagnosticsSubscriber(), new EfCoreDiagnosticsSubscriber(), new SqlClientDiagnosticSubscriber(),
			new AspNetCoreErrorDiagnosticsSubscriber(), new AspNetCorePageLoadDiagnosticSubscriber());
	}
}
