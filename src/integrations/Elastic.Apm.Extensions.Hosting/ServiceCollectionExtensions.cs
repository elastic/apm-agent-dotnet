// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Extensions.Hosting;
using Elastic.Apm.Extensions.Hosting.Config;
using Elastic.Apm.Extensions.Logging;
using Elastic.Apm.Logging;
using Elastic.Apm.NetCoreAll;
using Elastic.Apm.Report;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Register Elastic APM .NET Agent into the dependency injection container.
	/// You can customize the agent by passing additional <see cref="IDiagnosticsSubscriber"/> components to this method.
	/// <para>
	///   Use this method if you want to control what tracing capability of the agent you would like to use
	///   or in case you want to minimize the number of dependencies added to your application.
	/// </para>
	/// <para>
	///   If you want to simply enable every tracing component without configuration please use the
	///   <c>AddAllElasticApm</c> extension method from the <see href="https://www.nuget.org/packages/Elastic.Apm.NetCoreAll">Elastic.Apm.NetCoreAll package</see>.
	/// </para>
	/// </summary>
	/// <param name="services">An <see cref="IServiceCollection"/> where services are to be registered.</param>
	/// <param name="subscribers">Specify zero or more diagnostic source subscribers to enable.</param>
	public static IServiceCollection AddElasticApm(this IServiceCollection services, params IDiagnosticsSubscriber[] subscribers)
	{
		services.AddSingleton<IApmAgent>(sp =>
		{
			var agentConfigured = Agent.IsConfigured;

			// If the agent singleton has already been configured, we use that instance,
			// regardless of when/where it was created. This ensures that we don't attempt to
			// create multiple agent instances in the same process, which would result in
			// errors in the logs. When used correctly, this should never happen and we
			// should always initialise a new agent here.
			if (agentConfigured)
				return Agent.Instance;

			var netCoreLogger = ApmExtensionsLogger.GetApmLogger(sp);
			var configuration = sp.GetService<Configuration.IConfiguration>();
			var environmentName = GetDefaultEnvironmentName(sp);

			IConfigurationReader configurationReader = configuration is null
				? new EnvironmentConfiguration(netCoreLogger)
				: new ApmConfiguration(configuration, netCoreLogger, environmentName ?? "Undetermined");

			var globalLogger = AgentComponents.GetGlobalLogger(netCoreLogger, configurationReader.LogLevel);

			var logger = globalLogger is TraceLogger g ? new CompositeLogger(g, netCoreLogger) : netCoreLogger;

			if (environmentName is null)
				logger?.Warning()?.Log("Failed to retrieve default hosting environment name");

			// This may be null, which is fine
			var payloadSender = sp.GetService<IPayloadSender>();

			var components = new AgentComponents(logger, configurationReader, payloadSender);
			HostBuilderExtensions.UpdateServiceInformation(components.Service);

			Agent.Setup(components);

			// Under expected usage, this will always be a new lazily created instance based
			// on the configuration and components above. Worst case, it will be the existing
			// instance if another thread has created one since we checked at the start of this
			// method.
			var agent = Agent.Instance;

			// If the configuration is disabled, we don't want to subscribe any listeners.
			// We simply log a message and return the agent as-is.
			if (!agent.Configuration.Enabled)
			{
				logger?.Info()?.Log("The 'Enabled' agent config is set to false - the agent won't collect and send any data.");
			}
			else
			{
				// Subscribe handles cases where subscribers is null or empty, so we avoid
				// repeating that check here.
				agent.Subscribe(subscribers);
			}

			var loggerFactory = sp.GetService<ILoggerFactory>();
			loggerFactory.AddProvider(new ApmErrorLoggingProvider(agent));

			return agent;
		});

		// The ITracer is registered as a singleton to allow for easy access to the tracer
		// via dependency injection. This is useful for manual instrumentation.
		services.AddSingleton(sp => sp.GetRequiredService<IApmAgent>().Tracer);

		// This service is registered to trigger the creation of the IApmAgent.
		services.AddHostedService<ApmService>();

		return services;
	}

	private static string GetDefaultEnvironmentName(IServiceProvider serviceProvider) =>
#if NET6_0_OR_GREATER
		(serviceProvider.GetService(typeof(IHostEnvironment)) as IHostEnvironment)?.EnvironmentName; // This is preferred since 3.0
#else
#pragma warning disable CS0246
		(serviceProvider.GetService(typeof(IHostingEnvironment)) as IHostingEnvironment)?.EnvironmentName;
#pragma warning restore CS0246
#endif
}
