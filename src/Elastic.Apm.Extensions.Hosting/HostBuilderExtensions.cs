using System;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions.Hosting.Config;
using Elastic.Apm.Extensions.Logging;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elastic.Apm.Extensions.Hosting
{
	public static class HostBuilderExtensions
	{
		internal static string GetHostingEnvironmentName(HostBuilderContext ctx, IApmLogger logger)
		{
			try
			{
				var propertyInfo = ctx.GetType().GetProperty("HostingEnvironment");
				var hostingEnvironment = propertyInfo.GetValue(ctx, null);
				propertyInfo = hostingEnvironment.GetType().GetProperty("EnvironmentName");
				return propertyInfo.GetValue(hostingEnvironment, null) as string;
			}
			catch (Exception e)
			{
				logger?.Warning()?.LogException(e, "Failed to retrieve hosting environment name");
			}
			return null;
		}

		/// <summary>
		///  Register Elastic APM .NET Agent with components in the container.
		///  You can customize the agent by passing additional IDiagnosticsSubscriber components to this method.
		///  Use this method if you want to control what tracing capability of the agent you would like to use
		///  or in case you want to minimize the number of dependencies added to your application.
		///  If you want to simply enable every tracing component without configuration please use the
		///  UseAllElasticApm extension method from the Elastic.Apm.NetCoreAll package.
		/// </summary>
		/// <param name="builder">Builder.</param>
		/// <param name="subscribers">Specify which diagnostic source subscribers you want to connect.</param>
		public static IHostBuilder UseElasticApm(this IHostBuilder builder, params IDiagnosticsSubscriber[] subscribers) => UseElasticApm(builder, null, subscribers);

		internal static IHostBuilder UseElasticApm(this IHostBuilder builder, IPayloadSender payloadSender, params IDiagnosticsSubscriber[] subscribers)
		{
			builder.ConfigureServices((ctx, services) =>
			{
				IApmLogger logger;
				IConfigurationReader configReader;
				AgentComponents agentComponents;

				// If the static agent doesn't exist, we create one here. If there is already 1 agent created, we reuse it.
				if (!Agent.IsConfigured)
				{
					logger = new NetCoreLogger(new LoggerFactory());  // TODO: Could this be a problem?
					configReader = new MicrosoftExtensionsConfig(ctx.Configuration, logger, GetHostingEnvironmentName(ctx, logger));
					agentComponents = new AgentComponents(logger, configReader, payloadSender);
					UpdateServiceInformation(agentComponents.Service);
					Agent.Setup(agentComponents);
				}
				else
				{
					logger = Agent.Instance.Logger;
					configReader = Agent.Instance.ConfigurationReader;
					agentComponents = Agent.Instance.Components;
				}

				services.AddSingleton(logger);
				services.AddSingleton(configReader);
				services.AddSingleton(agentComponents);
				services.AddSingleton<IApmAgent>(Agent.Instance);
				services.AddSingleton(sp => sp.GetRequiredService<IApmAgent>().Tracer);

				// Create agent
				if (!(Agent.Instance is ApmAgent apmAgent)) return;

				if (!Agent.IsConfigured || !apmAgent.ConfigurationReader.Enabled) return;

				// Only add ElasticApmErrorLoggingProvider after the agent is created, because it depends on the agent
				services.AddSingleton<ILoggerProvider, ApmErrorLoggingProvider>(sp =>
					new ApmErrorLoggingProvider(sp.GetService<IApmAgent>()));

				if (subscribers != null && subscribers.Any() && Agent.IsConfigured)
					apmAgent.Subscribe(subscribers);
			});

			return builder;
		}

		internal static void UpdateServiceInformation(Service service)
		{
			var aspNetCoreVersion = GetAssemblyVersion("Microsoft.AspNetCore");
			var hostingVersion = GetAssemblyVersion("Microsoft.Extensions.Hosting");
			var version = aspNetCoreVersion ?? hostingVersion ?? "n/a";

			service.Framework = new Framework { Name = aspNetCoreVersion != null ? "ASP.NET Core" : ".NET Core", Version = version };
			service.Language = new Language { Name = "C#" }; //TODO
		}

		private static string GetAssemblyVersion(string assemblyName)
		{
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			var assembly = assemblies.FirstOrDefault(n => n.GetName().Name == assemblyName);
			if (assembly != null)
				return assembly.GetName().Version?.ToString();

			// if no exact match, try to find first assembly name that starts with given name
			assembly = assemblies
				.FirstOrDefault(n =>
				{
					var name = n.GetName().Name;
					return name != null && name.StartsWith(assemblyName);
				});

			return assembly?.GetName().Version?.ToString();
		}
	}
}
