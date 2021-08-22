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
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.SqlClient;
using ElasticApmStartupHook;

namespace Elastic.Apm.StartupHook.Loader
{
	/// <summary>
	/// Starts the agent
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
		/// Initializes and starts the agent
		/// </summary>
		public static void Initialize()
		{
			Agent.Setup(new AgentComponents());

			var logger = StartupHookLogger.Create();
			LoadDiagnosticSubscriber(new HttpDiagnosticsSubscriber(), logger);
			LoadDiagnosticSubscriber(new AspNetCoreDiagnosticSubscriber(), logger);
			LoadDiagnosticSubscriber(new EfCoreDiagnosticsSubscriber(), logger);
			LoadDiagnosticSubscriber(new SqlClientDiagnosticSubscriber(), logger);
			LoadDiagnosticSubscriber(new ElasticsearchDiagnosticsSubscriber(), logger);
			LoadDiagnosticSubscriber(new GrpcClientDiagnosticSubscriber(), logger);

			HostBuilderExtensions.UpdateServiceInformation(Agent.Instance.Service);

			static void LoadDiagnosticSubscriber(IDiagnosticsSubscriber diagnosticsSubscriber, StartupHookLogger logger)
			{
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
