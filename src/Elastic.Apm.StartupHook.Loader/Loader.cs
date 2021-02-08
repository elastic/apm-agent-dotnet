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
	/// <summary>
	/// Loads the agent assemblies, its dependent assemblies and starts it
	/// </summary>
	internal class Loader
	{
		private static AgentLoadContext _agentLoadContext;
		private static readonly StartupHookLogger Logger = StartupHookLogger.Create();

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
			_agentLoadContext = new AgentLoadContext(AssemblyDirectory, Logger);

			foreach (var libToLoad in AgentLoadContext.AgentDependencyLibsToLoad)
				LoadAssembly(libToLoad);
			foreach (var libToLoad in AgentLoadContext.AgentLibsToLoad)
				LoadAssembly(libToLoad);

			AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
			{
				if (AgentLoadContext.AgentDependencyLibsToLoad.Contains(assemblyName.Name)
					|| AgentLoadContext.AgentLibsToLoad.Contains(assemblyName.Name))
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
			if (AppDomain.CurrentDomain.GetAssemblies().Any(n => n.FullName.Equals(assemblyName, StringComparison.Ordinal)))
			{
				Logger.WriteLine($"{assemblyName} is alrady loaded - we don't try to load it");
				return;
			}

			try
			{
				var path = Path.Combine(AssemblyDirectory, assemblyName + ".dll");
				Logger.WriteLine($"Try loading: {path}");
				_agentLoadContext.LoadFromAssemblyName(new AssemblyName(assemblyName));
				Logger.WriteLine($"Loaded {path}");
			}
			catch (Exception e)
			{
				Logger.WriteLine($"Failed loading {assemblyName} - Exception: {e.GetType()}, message: {e.Message}");
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
					Logger.WriteLine($"Successfully Subscribed to {diagnosticsSubscriber.GetType().Name}");
				}
				catch (Exception e)
				{
					Logger.WriteLine($"Failed subscribing to {diagnosticsSubscriber.GetType().Name}, " +
						$"Exception {e}");
				}
			}
		}
	}
}
