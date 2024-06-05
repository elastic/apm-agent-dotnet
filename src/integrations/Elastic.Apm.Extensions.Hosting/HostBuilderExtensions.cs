// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
		[Obsolete("This extension is maintained for backward compatibility." +
			" We recommend registering the agent via the IServiceCollection using the AddElasticApm extension method instead. This method may be removed in a future release.")]
		public static IHostBuilder UseElasticApm(this IHostBuilder builder, params IDiagnosticsSubscriber[] subscribers)
		{
			builder.ConfigureServices((ctx, services) =>
			{
				//If the static agent doesn't exist, we create one here. If there is already 1 agent created, we reuse it.
				if (!Agent.IsConfigured)
				{
					services.AddSingleton<IApmLogger, NetCoreLogger>();
					services.AddSingleton<IConfigurationReader>(sp =>
						new ApmConfiguration(ctx.Configuration, sp.GetService<IApmLogger>(), GetHostingEnvironmentName(ctx, sp.GetService<IApmLogger>())));
				}
				else
				{
					services.AddSingleton(Agent.Instance.Logger);
					services.AddSingleton(Agent.Instance.Configuration);
				}

				services.AddSingleton(sp =>
				{
					if (Agent.IsConfigured)
						return Agent.Components;

					var logger = sp.GetService<IApmLogger>();
					var configReader = sp.GetService<IConfigurationReader>();

					var payloadSender = sp.GetService<IPayloadSender>();

					var components = new AgentComponents(logger, configReader, payloadSender);
					UpdateServiceInformation(components.Service);
					return components;
				});

				services.AddSingleton<IApmAgent, ApmAgent>(sp =>
				{
					if (Agent.IsConfigured)
						return Agent.Instance;

					Agent.Setup(sp.GetService<AgentComponents>());
					return Agent.Instance;
				});

				services.AddSingleton(sp => sp.GetRequiredService<IApmAgent>().Tracer);

				// Force to create agent
				var serviceProvider = services.BuildServiceProvider();
				var agent = serviceProvider.GetService<IApmAgent>();

				if (!(agent is ApmAgent apmAgent))
					return;

				if (!Agent.IsConfigured || !apmAgent.Configuration.Enabled)
					return;

				// Only add ElasticApmErrorLoggingProvider after the agent is created, because it depends on the agent
				services.AddSingleton<ILoggerProvider, ApmErrorLoggingProvider>(sp =>
					new ApmErrorLoggingProvider(apmAgent));

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
			var isCore = false;

			if (aspNetCoreVersion is null && hostingVersion is not null && Version.TryParse(hostingVersion, out var v))
				isCore = v.Major < 5;

			service.Framework = new Framework { Name = aspNetCoreVersion != null ? "ASP.NET Core" : isCore ? ".NET Core" : ".NET", Version = version };
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
