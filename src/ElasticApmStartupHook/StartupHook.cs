using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Elastic.Apm;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.SqlClient;
using Elastic.Apm.Elasticsearch;

// ReSharper disable once CheckNamespace - per doc. this must be called StartupHook without a namespace with an Initialize method.
internal class StartupHook
{
	public static void Initialize()
	{
		var agentLibsToLoad =  new[]{ "Elastic.Apm", "Elastic.Apm.Extensions.Logging", "Elastic.Apm.AspNetCore", "Elastic.Apm.EntityFrameworkCore", "Elastic.Apm.SqlClient", "Elastic.Apm.Elasticsearch" };
		var agentDependencyLibsToLoad = new[] { "System.Diagnostics.PerformanceCounter", "Microsoft.Diagnostics.Tracing.TraceEvent", "Newtonsoft.Json", "Elasticsearch.Net" };

		var startupHookEnvVar = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");

		if (string.IsNullOrEmpty(startupHookEnvVar))
			return;

		var lastIndex = startupHookEnvVar.LastIndexOf(Path.DirectorySeparatorChar);
		var agentDir = startupHookEnvVar.Substring(0, lastIndex+1);


		foreach (var libToLoad in agentDependencyLibsToLoad) AssemblyLoadContext.Default.LoadFromAssemblyPath(agentDir + libToLoad + ".dll");
		foreach (var libToLoad in agentLibsToLoad) AssemblyLoadContext.Default.LoadFromAssemblyPath(agentDir + libToLoad + ".dll");

		StartAgent();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void StartAgent()
	{
		Agent.Setup(new AgentComponents());
		Agent.Subscribe(new HttpDiagnosticsSubscriber());

		if (AppDomain.CurrentDomain.GetAssemblies().Any(n => n.GetName().Name.Contains("Microsoft.AspNetCore.")))
		{
			Agent.Subscribe(
				new AspNetCoreErrorDiagnosticsSubscriber(),
				new AspNetCoreDiagnosticSubscriber(),
				new EfCoreDiagnosticsSubscriber(),
				new SqlClientDiagnosticSubscriber(),
				new ElasticsearchDiagnosticsSubscriber()
			);
		}
	}
}
