// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Reflection;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Elasticsearch;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.GrpcClient;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.Logging;
using IApmLogger = Elastic.Apm.Logging.IApmLogger;

namespace Elastic.Apm.StartupHook.Loader
{
	/// <summary>
	/// Starts the agent
	/// </summary>
	internal class Loader
	{
		/// <summary>
		/// Initializes and starts the agent
		/// </summary>
		public static void Initialize()
		{
			var agentComponents = new AgentComponents();
			Agent.Setup(agentComponents);

			var logger = agentComponents.Logger;
			LoadDiagnosticSubscriber(new HttpDiagnosticsSubscriber(), logger);
			LoadDiagnosticSubscriber(new AspNetCoreDiagnosticSubscriber(), logger);
			LoadDiagnosticSubscriber(new EfCoreDiagnosticsSubscriber(), logger);
			LoadDiagnosticSubscriber(new SqlClientDiagnosticSubscriber(), logger);
			LoadDiagnosticSubscriber(new ElasticsearchDiagnosticsSubscriber(), logger);
			LoadDiagnosticSubscriber(new GrpcClientDiagnosticSubscriber(), logger);

			HostBuilderExtensions.UpdateServiceInformation(Agent.Instance.Service);

			static void LoadDiagnosticSubscriber(IDiagnosticsSubscriber diagnosticsSubscriber, IApmLogger logger)
			{
				try
				{
					Agent.Subscribe(diagnosticsSubscriber);
				}
				catch (Exception e)
				{
					logger.Error()?.LogException(e, $"Failed subscribing to {diagnosticsSubscriber.GetType().Name}");
				}
			}
		}
	}
}
