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
				Logger.WriteLine($"Default load context resolver, resolving {assemblyName.Name} from AgentLoadContext");
				return _agentLoadContext.LoadFromAssemblyName(new AssemblyName(assemblyName.Name));
			};

			try
			{
				StartAgent();
			}
			catch (Exception e)
			{
				Logger.WriteLine($"Failed laoding the agent, exception: {e}");
			}
		}

		public static void LoadAssembly(string assemblyName)
		{
			if (AppDomain.CurrentDomain.GetAssemblies().Any(n => n.FullName.Equals(assemblyName, StringComparison.Ordinal)))
			{
				Logger.WriteLine($"{assemblyName} is already loaded - we don't try to load it");
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

		/// <summary>
		/// Calls <code>Agent.Setup</code> and subscribs to all diagnostic subscribers.
		/// To avoid loading any agent type into the default AssemblyLoadContext everything is done through reflection.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void StartAgent()
		{
			var apmAssembly = _agentLoadContext.LoadFromAssemblyName(new AssemblyName("Elastic.Apm"));
			var agentType = apmAssembly.GetType("Elastic.Apm.Agent");

			var AgentComponentsType = apmAssembly.GetType("Elastic.Apm.AgentComponents");
			var agentComponentsInstance = Activator.CreateInstance(AgentComponentsType, null, null, null);

			var setupMethod = agentType.GetMethod("Setup", BindingFlags.Public | BindingFlags.Static);
			setupMethod.Invoke(null, new[] { agentComponentsInstance });

			var subscribers = new (string subscriberName, string containingSssemblyName)[]
			{
				("Elastic.Apm.DiagnosticSource.HttpDiagnosticsSubscriber", "Elastic.Apm"),
				("Elastic.Apm.AspNetCore.DiagnosticListener.AspNetCoreDiagnosticSubscriber", "Elastic.Apm.AspNetCore"),
				("Elastic.Apm.EntityFrameworkCore.EfCoreDiagnosticsSubscriber", "Elastic.Apm.EntityFrameworkCore"),
				("Elastic.Apm.SqlClient.SqlClientDiagnosticSubscriber", "Elastic.Apm.SqlClient"),
				("Elastic.Apm.Elasticsearch.ElasticsearchDiagnosticsSubscriber", "Elastic.Apm.Elasticsearch"),
				("Elastic.Apm.GrpcClient.GrpcClientDiagnosticSubscriber", "Elastic.Apm.GrpcClient")
			};

			var subscribeMethod = agentType.GetMethod("Subscribe", BindingFlags.Public | BindingFlags.Static);

			var subscriberType = apmAssembly.GetType("Elastic.Apm.DiagnosticSource.IDiagnosticsSubscriber");
			var subscriberArray = Array.CreateInstance(subscriberType, 1);

			foreach (var (subscriberName, containingAssemblyName) in subscribers)
			{
				var assembly = _agentLoadContext.LoadFromAssemblyName(new AssemblyName(containingAssemblyName));
				var subscriberInstance = Activator.CreateInstance(assembly.GetType(subscriberName));

				subscriberArray.SetValue(subscriberInstance, 0);

				try
				{
					subscribeMethod.Invoke(null, new object[] { subscriberArray });
					Logger.WriteLine($"Successfully Subscribed to {subscriberName}");
				}
				catch (Exception e)
				{
					Logger.WriteLine($"Failed subscribing to {subscriberName}, " +
						$"Exception: {e}");
				}
			}
		}
	}
}
