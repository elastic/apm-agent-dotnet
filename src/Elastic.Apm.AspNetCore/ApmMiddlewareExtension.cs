// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Extensions.Hosting.Config;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Elastic.Apm.AspNetCore
{
	public static class ApmMiddlewareExtension
	{
		/// <summary>
		/// Adds the Elastic APM Middleware to the ASP.NET Core pipeline.
		/// You can customize the agent by passing additional IDiagnosticsSubscriber components to this method.
		/// Use this method if you want to control what tracing capability of the agent you would like to use
		/// or in case you want to minimize the number of dependencies added to your application.
		/// Please note that by default without additional parameters this method only enables ASP.NET Core
		/// monitoring - e.g. database statements or outgoing HTTP calls won't be traced.
		/// If you want to simply enable every tracing component without configuration please use the
		/// UseAllElasticApm extension method from the Elastic.Apm.NetCoreAll package.
		/// </summary>
		/// <returns>The elastic apm.</returns>
		/// <param name="builder">Builder.</param>
		/// <param name="configuration">
		/// You can optionally pass the IConfiguration of your application to the Elastic APM Agent. By
		/// doing this the agent will read agent related configurations through this IConfiguration instance.
		/// If no <see cref="Microsoft.Extensions.Configuration.IConfiguration" /> is passed to the agent then it will read configs from environment variables.
		/// </param>
		/// <param name="subscribers">
		/// Specify which diagnostic source subscribers you want to connect. The
		/// <see cref="AspNetCoreErrorDiagnosticsSubscriber" /> is by default enabled.
		/// </param>
		public static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder,
			IConfiguration configuration = null,
			params IDiagnosticsSubscriber[] subscribers
		)
		{
			var logger = builder.ApplicationServices.GetApmLogger();

			var configReader = configuration == null
				? new EnvironmentConfigurationReader(logger)
				: new MicrosoftExtensionsConfig(configuration, logger, builder.ApplicationServices.GetEnvironmentName()) as IConfigurationReader;

			var config = new AgentComponents(configurationReader: configReader, logger: logger);
			HostBuilderExtensions.UpdateServiceInformation(config.Service);

			// Agent.Setup must be called, even if agent is disabled. This way static public API usage won't implicitly initialize an agent with default values, instead, this will be reused.
			Agent.Setup(config);

			return UseElasticApm(builder, Agent.Instance, logger, subscribers);
		}

		internal static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder,
			ApmAgent agent,
			IApmLogger logger,
			params IDiagnosticsSubscriber[] subscribers
		)
		{
			if (!agent.ConfigurationReader.Enabled)
			{
				if (!Agent.IsConfigured)
					Agent.Setup(agent);

				logger?.Info()?.Log("The 'Enabled' agent config is set to false - the agent won't collect and send any data.");
				return builder;
			}

			var subs = subscribers?.ToList() ?? new List<IDiagnosticsSubscriber>(1);

			if (subs.Count == 0 || subs.All(s => s.GetType() != typeof(AspNetCoreErrorDiagnosticsSubscriber)))
				subs.Add(new AspNetCoreErrorDiagnosticsSubscriber());

			agent.Subscribe(subs.ToArray());
			return builder.UseMiddleware<ApmMiddleware>(agent.Tracer, agent);
		}

		internal static string GetEnvironmentName(this IServiceProvider serviceProvider) =>
#if NETCOREAPP3_0 || NET5_0
			(serviceProvider.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment)?.EnvironmentName;
#else
			(serviceProvider.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment)?.EnvironmentName;
#endif


		internal static IApmLogger GetApmLogger(this IServiceProvider serviceProvider) =>
			serviceProvider.GetService(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory
				? (IApmLogger)new NetCoreLogger(loggerFactory)
				: ConsoleLogger.Instance;
	}
}
