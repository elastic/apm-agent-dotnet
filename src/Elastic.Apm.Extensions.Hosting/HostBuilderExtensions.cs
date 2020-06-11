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
		public static IHostBuilder UseElasticApm(this IHostBuilder builder, params IDiagnosticsSubscriber[] subscribers)
		{
			builder.ConfigureServices((ctx, services) =>
			{
				services.AddSingleton<IApmLogger, NetCoreLogger>();
				services.AddSingleton<IConfigurationReader>(sp =>
					new MicrosoftExtensionsConfig(ctx.Configuration, sp.GetService<IApmLogger>(), ctx.HostingEnvironment.EnvironmentName));

				services.AddSingleton(sp =>
				{
					var components = new AgentComponents(sp.GetService<IApmLogger>(), sp.GetService<IConfigurationReader>());
					UpdateServiceInformation(components.Service);
					return components;
				});
				services.AddSingleton<IApmAgent, ApmAgent>(sp =>
				{
					var apmAgent = new ApmAgent(sp.GetService<AgentComponents>());
					if (subscribers != null && subscribers.Any()) apmAgent.Subscribe(subscribers);
					return apmAgent;
				});
			});

			return builder;
		}

		internal static void UpdateServiceInformation(Service service)
		{
			string version;
			// todo: WILL CHANGE IT
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

			service.Framework = new Framework { Name = ".NET Core", Version = version };
			service.Language = new Language { Name = "C#" }; //TODO
		}
	}
}
