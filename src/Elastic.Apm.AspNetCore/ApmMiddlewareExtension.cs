using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.AspNetCore.DiagnosticListener;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore
{
	public static class ApmMiddlewareExtension
	{
		/// <summary>
		/// Adds the Elastic APM Middleware to the ASP.NET Core pipeline.
		/// </summary>
		/// <returns>The Elastic APM.</returns>
		/// <param name="builder">Builder.</param>
		/// <param name="configuration">
		/// You can optionally pass the IConfiguration of your application to the Elastic APM Agent. By doing this the agent will read agent related
		/// configurations through this IConfiguration instance.
		/// If no <see cref="IConfiguration" /> is passed to the agent then it will read configs from environment variables.
		/// </param>
		/// <param name="subscribers">
		/// Specify which diagnostic source subscribers you want to connect. The  <see cref="AspNetCoreDiagnosticsSubscriber" /> is enabled by default.
		/// </param>
		public static IApplicationBuilder UseElasticApm(this IApplicationBuilder builder, IConfiguration configuration = null, params IDiagnosticsSubscriber[] subscribers)
		{
			var logger = ConsoleLogger.Instance;
			var configReader = configuration == null
				? new EnvironmentConfigurationReader(logger)
				: new ApplicationConfigurationReader(configuration, logger) as IConfigurationReader;

			var config = new AgentComponents(configurationReader: configReader);
			UpdateServiceInformation(config.Service);

			Agent.Setup(config);
			return UseElasticApm(builder, Agent.Instance, subscribers);
		}

		internal static IApplicationBuilder UseElasticApm(this IApplicationBuilder builder, ApmAgent agent, params IDiagnosticsSubscriber[] subscribers)
		{
			agent.Subscribe(new List<IDiagnosticsSubscriber>(subscribers ?? Array.Empty<IDiagnosticsSubscriber>())
			{
				new AspNetCoreDiagnosticsSubscriber()
			}.ToArray());

			return builder.UseMiddleware<ApmMiddleware>(agent.Tracer, agent);
		}

		internal static void UpdateServiceInformation(Service service)
		{
			string version;
			var versionQuery = AppDomain.CurrentDomain.GetAssemblies().Where(n => n.GetName().Name == "Microsoft.AspNetCore");
			var assemblies = versionQuery as Assembly[] ?? versionQuery.ToArray();

			if (assemblies.Any())
			{
				version = assemblies.First().GetName().Version.ToString();
			}
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
