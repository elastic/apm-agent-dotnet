// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.SqlClient;
using ElasticApmStartupHook;

namespace Elastic.Apm.StartupHook.Loader
{

	internal class AgentLoadContext : AssemblyLoadContext
	{
		public static string[] agentLibsToLoad = new[] { "Elastic.Apm", "Elastic.Apm.Extensions.Hosting", "Elastic.Apm.AspNetCore", "Elastic.Apm.EntityFrameworkCore", "Elastic.Apm.SqlClient", "Elastic.Apm.GrpcClient", "Elastic.Apm.Elasticsearch" };
		public static string[] agentDependencyLibsToLoad = new[] { "System.Diagnostics.PerformanceCounter", "Microsoft.Diagnostics.Tracing.TraceEvent", "Newtonsoft.Json", "Elasticsearch.Net" };

		private readonly StartupHookLogger _logger;
		private readonly string _asssemblyDir;

		public AgentLoadContext(string assemblyDir, StartupHookLogger logger) => (_asssemblyDir, _logger) = (assemblyDir, logger);

		protected override Assembly Load(AssemblyName assemblyName)
		{
			try
			{
				if (agentLibsToLoad.Contains(assemblyName.Name) || agentDependencyLibsToLoad.Contains(assemblyName.Name))
				{
					var path = Path.Combine(_asssemblyDir, assemblyName.Name + ".dll");
					return LoadFromAssemblyPath(path);
				}
				else
				{
					// If it's not an agent assembly or an agent dependency, let's just reuse it from the default load context
					return Default.LoadFromAssemblyName(assemblyName);
				}

			}
			catch (Exception e)
			{
				_logger.WriteLine($"{nameof(AgentLoadContext)} Failed loading {assemblyName.Name}, exception: {e}");
			}

			return null;
		}
	}

	/// <summary>
	/// Loads the agent assemblies, its dependent assemblies and starts it
	/// </summary>
	internal class Loader
	{
		private static readonly StartupHookLogger _logger = StartupHookLogger.Create();

		private static AgentLoadContext _agentLoadContext;

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
			_agentLoadContext = new AgentLoadContext(AssemblyDirectory, _logger);

			foreach (var libToLoad in AgentLoadContext.agentDependencyLibsToLoad)
				LoadAssembly(libToLoad);
			foreach (var libToLoad in AgentLoadContext.agentLibsToLoad)
				LoadAssembly(libToLoad);

			AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
			{
				if (AgentLoadContext.agentDependencyLibsToLoad.Contains(assemblyName.Name) || AgentLoadContext.agentLibsToLoad.Contains(assemblyName.Name))
				{
					// If AssemblyLoadContext.Default tries to load an agent assembly or one of its dependencies, we return it from _agentLoadContext
					return _agentLoadContext.LoadFromAssemblyName(new AssemblyName(assemblyName.Name));
				}

				return context.LoadFromAssemblyName(assemblyName);
			};

			StartAgent();
		}

		public static void LoadAssembly(string assemblyName)
		{
			if (AppDomain.CurrentDomain.GetAssemblies().Any(n => n.FullName == assemblyName))
			{
				_logger.WriteLine($"{assemblyName} is alrady loaded - we don't try to load it");
				return;
			}

			try
			{
				var path = Path.Combine(AssemblyDirectory, assemblyName + ".dll");
				_logger.WriteLine($"Try loading: {path}");
				_agentLoadContext.LoadFromAssemblyName(new AssemblyName(assemblyName));

				_logger.WriteLine($"Loaded {path}");
			}
			catch (Exception e)
			{
				_logger.WriteLine($"Failed loading {assemblyName} - Exception: {e.GetType()}, message: {e.Message}");
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StartAgent()
		{
			Agent.Setup(new AgentComponents());

			LoadDiagnosticSubscriber(new HttpDiagnosticsSubscriber());
			LoadDiagnosticSubscriber(new AspNetCoreDiagnosticSubscriber());
			LoadDiagnosticSubscriber(new EfCoreDiagnosticsSubscriber());
			LoadDiagnosticSubscriber(new SqlClientDiagnosticSubscriber());
			LoadDiagnosticSubscriber(new ElasticsearchDiagnosticsSubscriber());
			LoadDiagnosticSubscriber(new GrpcClientDiagnosticSubscriber());

			static void LoadDiagnosticSubscriber(IDiagnosticsSubscriber diagnosticsSubscriber)
			{
				try
				{
					Agent.Subscribe(diagnosticsSubscriber);
					_logger.WriteLine($"Successfully Subscribed to {diagnosticsSubscriber.GetType().Name}");
				}
				catch (Exception e)
				{
					_logger.WriteLine($"Failed subscribing to {diagnosticsSubscriber.GetType().Name}, " +
						$"Exception {e}");
				}
			}
		}
	}
}
