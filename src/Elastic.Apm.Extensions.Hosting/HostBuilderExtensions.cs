using System;
using System.Linq;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions.Hosting.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elastic.Apm.Extensions.Hosting
{
	public static class HostBuilderExtensions
	{
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
		public static IHostBuilder UseElasticApm(this IHostBuilder builder, params IDiagnosticsSubscriber[] subscribers)
		{
			builder.ConfigureServices((ctx, services) =>
			{
				//If the static agent doesn't exist, we create one here. If there is already 1 agent created, we reuse it.

				if (!Agent.IsConfigured)
				{
					services.AddSingleton<IApmLogger, NetCoreLogger>();
					services.AddSingleton<IConfigurationReader>(sp =>
					new MicrosoftExtensionsConfig(ctx.Configuration, sp.GetService<IApmLogger>(), ctx.HostingEnvironment.EnvironmentName));
				}
				else
				{
					services.AddSingleton(Agent.Instance.Logger);
					services.AddSingleton(Agent.Instance.ConfigurationReader);
				}

				services.AddSingleton(sp =>
				{
					if (Agent.IsConfigured) return Agent.Components;

					var logger = sp.GetService<IApmLogger>();
					var configReader = sp.GetService<IConfigurationReader>();

					var components = new AgentComponents(logger, configReader);
					UpdateServiceInformation(components.Service);
					return components;
				});

				services.AddSingleton<IApmAgent, ApmAgent>(sp =>
				{
					if (Agent.IsConfigured) return Agent.Instance;

					var apmAgent = new ApmAgent(sp.GetService<AgentComponents>());
					Agent.Setup(sp.GetService<AgentComponents>());
					return apmAgent;
				});

				if(Agent.IsConfigured && Agent.Config.Enabled)
					if (subscribers != null && subscribers.Any() && Agent.IsConfigured) Agent.Subscribe(subscribers);

				services.AddSingleton(sp => sp.GetRequiredService<IApmAgent>().Tracer);
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
			var versionQuery = AppDomain.CurrentDomain.GetAssemblies().Where(n => n.GetName().Name == assemblyName);
			var assemblies = versionQuery as Assembly[] ?? versionQuery.ToArray();
			if (assemblies.Any()) return assemblies.First().GetName().Version?.ToString();

			versionQuery = AppDomain.CurrentDomain.GetAssemblies().Where(n =>
			{
				var name = n.GetName().Name;
				return name != null && name.Contains(assemblyName);
			});
			var enumerable = versionQuery as Assembly[] ?? versionQuery.ToArray();
			return enumerable.Any() ? enumerable.FirstOrDefault()?.GetName().Version?.ToString() : null;
		}
	}
}
