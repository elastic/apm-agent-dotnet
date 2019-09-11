using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
		/// If no <see cref="IConfiguration" /> is passed to the agent then it will read configs from environment variables.
		/// </param>
		/// <param name="subscribers">
		/// Specify which diagnostic source subscribers you want to connect. The
		/// <see cref="AspNetCoreDiagnosticsSubscriber" /> is by default enabled.
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
			UpdateServiceInformation(config.Service);

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
			var subs = new List<IDiagnosticsSubscriber>(subscribers ?? Array.Empty<IDiagnosticsSubscriber>())
			{
				new AspNetCoreDiagnosticsSubscriber()
			};
			agent.Subscribe(subs.ToArray());
			return builder.UseMiddleware<ApmMiddleware>(agent.Tracer, agent);
		}

		internal static string GetEnvironmentName(this IServiceProvider serviceProvider) =>
			(serviceProvider.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment)?.EnvironmentName;

		internal static IApmLogger GetApmLogger(this IServiceProvider serviceProvider) =>
			serviceProvider.GetService(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory
				? (IApmLogger)new AspNetCoreLogger(loggerFactory)
				: ConsoleLogger.Instance;

		internal static void UpdateServiceInformation(Service service)
		{
			string version;
			var versionQuery = AppDomain.CurrentDomain.GetAssemblies().Where(n => n.GetName().Name == "Microsoft.AspNetCore");
			var assemblies = versionQuery as Assembly[] ?? versionQuery.ToArray();
			if (assemblies.Any())
				version = assemblies.First().GetName().Version.ToString();
			else
			{
				versionQuery = AppDomain.CurrentDomain.GetAssemblies().Where(n => n.GetName().Name.Contains("Microsoft.AspNetCore"));
				var enumerable = versionQuery as Assembly[] ?? versionQuery.ToArray();
				version = enumerable.Any() ? enumerable.FirstOrDefault()?.GetName().Version.ToString() : "n/a";
			}

			service.Framework = new Framework { Name = "ASP.NET Core", Version = version };
			service.Language = new Language { Name = "C#" }; //TODO
		}
	}
}
