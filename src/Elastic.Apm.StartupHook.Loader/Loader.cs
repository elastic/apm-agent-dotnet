// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.SqlClient;
using Elastic.Apm.StartupHook.Common;

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
				return Path.GetDirectoryName(location);
			}
		}

		/// <summary>
		/// Initializes assemblies and starts the agent
		/// </summary>
		public static void Initialize()
		{
			var agentLibsToLoad = new[] { "Elastic.Apm", "Elastic.Apm.Extensions.Hosting", "Elastic.Apm.AspNetCore", "Elastic.Apm.EntityFrameworkCore", "Elastic.Apm.SqlClient", "Elastic.Apm.GrpcClient", "Elastic.Apm.Elasticsearch" };
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

			LoadDiagnosticSubscriber(new AspNetCoreDiagnosticSubscriber());
			LoadDiagnosticSubscriber(new EfCoreDiagnosticsSubscriber());
			LoadDiagnosticSubscriber(new SqlClientDiagnosticSubscriber());
			LoadDiagnosticSubscriber(new ElasticsearchDiagnosticsSubscriber());
			LoadDiagnosticSubscriber(new GrpcClientDiagnosticSubscriber());

			void LoadDiagnosticSubscriber(IDiagnosticsSubscriber diagnosticsSubscriber)
			{
				var logger = StartupHookLogger.CreateLogger();
				try
				{
					Agent.Subscribe(diagnosticsSubscriber);
				}
				catch (Exception e)
				{
					logger.WriteLine($"Failed subscribing to {diagnosticsSubscriber.GetType().Name}, " +
						$"Exception type: {e.GetType().Name}, message: {e.Message}");
				}
			}
		}
	}
}
