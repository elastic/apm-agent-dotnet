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
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Elastic.Apm.AspNetCore
{
	public static class ApplicationBuilderExtensions
	{
		/// <summary>
		/// Sets up ASP.NET Core instrumentation to be sent over to Elastic APM.
		/// <para>
		/// You can customize the agent by passing additional IDiagnosticsSubscriber components to this method.
		/// Use this method if you want to control what tracing capability of the agent you would like to use
		/// or in case you want to minimize the number of dependencies added to your application.
		/// </para>
		/// <para>
		/// Please note that by default without additional parameters this method only enables ASP.NET Core
		/// monitoring - e.g. database statements or outgoing HTTP calls won't be traced.
		/// If you want to simply enable every tracing component without configuration please use the
		/// <code>UseAllElasticApm()</code> extension method from the <c>Elastic.Apm.NetCoreAll</c> package.
		/// </para>
		/// </summary>
		/// <returns>The elastic apm.</returns>
		/// <param name="builder">Builder.</param>
		/// <param name="configuration">
		/// You can optionally pass the IConfiguration of your application to the Elastic APM Agent. By
		/// doing this the agent will read agent related configurations through this IConfiguration instance.
		/// If no <see cref="IConfiguration" /> is passed to the agent then it will read configs from environment variables.
		/// </param>
		/// <param name="subscribers">
		/// Specify which diagnostic source subscribers you want to connect.
		/// <para>The <see cref="AspNetCoreDiagnosticSubscriber" /> will always be injected if not specified.</para>
		/// </param>
		[Obsolete("This extension is maintained for backward compatibility." +
			" We recommend registering the agent via the IServiceCollection using the AddElasticApm extension method instead. This method may be removed in a future release.")]
		public static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder,
			IConfiguration configuration = null,
			params IDiagnosticsSubscriber[] subscribers
		)
		{
			var logger = ApmExtensionsLogger.GetApmLogger(builder.ApplicationServices);

			var configReader = configuration == null
				? new EnvironmentConfiguration(logger)
				: new ApmConfiguration(configuration, logger, builder.ApplicationServices.GetEnvironmentName()) as IConfigurationReader;

			var config = new AgentComponents(configurationReader: configReader, logger: logger);
			HostBuilderExtensions.UpdateServiceInformation(config.Service);

			// Agent.Setup must be called, even if agent is disabled. This way static public API usage won't implicitly initialize an agent with default values, instead, this will be reused.
			Agent.Setup(config);

			return UseElasticApm(builder, Agent.Instance, logger, subscribers);
		}

		internal static IApplicationBuilder UseElasticApm(
			this IApplicationBuilder builder,
			ApmAgent agent,
			Logging.IApmLogger logger,
			params IDiagnosticsSubscriber[] subscribers
		)
		{
			if (!agent.Configuration.Enabled)
			{
				if (!Agent.IsConfigured)
					Agent.Setup(agent);

				logger?.Info()?.Log("The 'Enabled' agent config is set to false - the agent won't collect and send any data.");
				return builder;
			}

			var subs = subscribers?.ToList() ?? new List<IDiagnosticsSubscriber>(1);

			if (subs.Count == 0 || subs.All(s => s.GetType() != typeof(AspNetCoreDiagnosticSubscriber)))
				subs.Add(new AspNetCoreDiagnosticSubscriber());

			agent.Subscribe(subs.ToArray());
			return builder;
		}

		private static string GetEnvironmentName(this IServiceProvider serviceProvider) =>
#if NET6_0_OR_GREATER
			(serviceProvider.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment)?.EnvironmentName;
#else
#pragma warning disable CS0246
			(serviceProvider.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment)?.EnvironmentName;
#pragma warning restore CS0246
#endif
	}
}
