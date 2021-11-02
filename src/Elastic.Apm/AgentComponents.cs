// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.DiagnosticListeners;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm
{
	public class AgentComponents : IApmAgent, IDisposable
	{
		public AgentComponents(
			IApmLogger logger = null,
			IConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null
		) : this(logger, configurationReader, payloadSender, null, null, null, null) { }

		internal AgentComponents(
			IApmLogger logger,
			IConfigurationReader configurationReader,
			IPayloadSender payloadSender,
			IMetricsCollector metricsCollector,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			ICentralConfigurationFetcher centralConfigurationFetcher,
			IApmServerInfo apmServerInfo,
			BreakdownMetricsProvider breakdownMetricsProvider = null
		)
		{
			try
			{
				var tempLogger = logger ?? ConsoleLogger.LoggerOrDefault(configurationReader?.LogLevel);
				ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(tempLogger);
				Logger = logger ?? ConsoleLogger.LoggerOrDefault(ConfigurationReader.LogLevel);
				Service = Service.GetDefaultService(ConfigurationReader, Logger);

				var systemInfoHelper = new SystemInfoHelper(Logger);
				var system = systemInfoHelper.GetSystemInfo(ConfigurationReader.HostName);

				ConfigurationStore = new ConfigurationStore(new ConfigurationSnapshotFromReader(ConfigurationReader, "local"), Logger);

				ApmServerInfo = apmServerInfo ?? new ApmServerInfo();

				PayloadSender = payloadSender
					?? new PayloadSenderV2(Logger, ConfigurationStore.CurrentSnapshot, Service, system, ApmServerInfo,
						isEnabled: ConfigurationReader.Enabled);

				if (ConfigurationReader.Enabled)
					breakdownMetricsProvider ??= new BreakdownMetricsProvider(Logger);

				HttpTraceConfiguration = new HttpTraceConfiguration();
				SubscribedListeners = new HashSet<Type>();

				// initialize the tracer before central configuration or metric collection is started
				TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigurationStore,
					currentExecutionSegmentsContainer ?? new CurrentExecutionSegmentsContainer(), ApmServerInfo, breakdownMetricsProvider);

				if (ConfigurationReader.Enabled)
				{
					CentralConfigurationFetcher = centralConfigurationFetcher ?? new CentralConfigurationFetcher(Logger, ConfigurationStore, Service);
					MetricsCollector = metricsCollector ?? new MetricsCollector(Logger, PayloadSender, ConfigurationStore, breakdownMetricsProvider);
					MetricsCollector.StartCollecting();
				}
				else
					Logger.Info()?.Log("The Elastic APM .NET Agent is disabled - the agent won't capture traces and metrics.");
			}
			catch (Exception e)
			{
				Logger.Error()?.LogException(e, "Failed initializing agent.");
			}
		}

		internal ICentralConfigurationFetcher CentralConfigurationFetcher { get; }

		internal IConfigurationStore ConfigurationStore { get; }

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		private IMetricsCollector MetricsCollector { get; }

		public IPayloadSender PayloadSender { get; }

		internal IApmServerInfo ApmServerInfo { get; }

		internal HttpTraceConfiguration HttpTraceConfiguration { get; }

		internal HashSet<Type> SubscribedListeners { get; }

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		public Service Service { get; }

		public ITracer Tracer => TracerInternal;

		internal Tracer TracerInternal { get; }

		public void Dispose()
		{
			if (MetricsCollector is IDisposable disposableMetricsCollector) disposableMetricsCollector.Dispose();

			if (PayloadSender is IDisposable disposablePayloadSender) disposablePayloadSender.Dispose();

			CentralConfigurationFetcher?.Dispose();
		}
	}
}
