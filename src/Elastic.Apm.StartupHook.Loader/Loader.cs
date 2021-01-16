using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.SqlClient;

namespace Elastic.Apm.StartupHook.Loader
{
	/// <summary>
	/// Loads the agent assemblies and its dependent assemblies and starts it
	/// </summary>
	internal class Loader
	{
		/// <summary>
		/// The directory in which the executing assembly is located
		/// </summary>
		private static string AssemblyDirectory
		{
			get
			{
				var location = Assembly.GetExecutingAssembly().Location;
				var builder = new UriBuilder(location);
				return Path.GetDirectoryName(builder.Uri.LocalPath);
			}
		}

		/// <summary>
		/// Initializes assemblies and starts the agent
		/// </summary>
		public static void Initialize()
		{
			var agentLibsToLoad =  new[]{ "Elastic.Apm", "Elastic.Apm.Extensions.Hosting", "Elastic.Apm.AspNetCore", "Elastic.Apm.EntityFrameworkCore", "Elastic.Apm.SqlClient", "Elastic.Apm.Elasticsearch" };
			var agentDependencyLibsToLoad = new[] { "System.Diagnostics.PerformanceCounter", "Microsoft.Diagnostics.Tracing.TraceEvent", "Newtonsoft.Json", "Elasticsearch.Net" };

			foreach (var libToLoad in agentDependencyLibsToLoad)
				AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AssemblyDirectory, libToLoad + ".dll"));
			foreach (var libToLoad in agentLibsToLoad)
				AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(AssemblyDirectory, libToLoad + ".dll"));

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
}
