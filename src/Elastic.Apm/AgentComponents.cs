// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
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
			ICentralConfigFetcher centralConfigFetcher,
			IApmServerInfo apmServerInfo
		)
		{
			try
			{

				var tempLogger = logger ?? ConsoleLogger.LoggerOrDefault(configurationReader?.LogLevel);
				ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(tempLogger);
				Logger = logger ?? ConsoleLogger.LoggerOrDefault(ConfigurationReader.LogLevel);
				Service = Service.GetDefaultService(ConfigurationReader, Logger);

				var systemInfoHelper = new SystemInfoHelper(Logger);
				var system = systemInfoHelper.ParseSystemInfo(ConfigurationReader.HostName);

				ConfigStore = new ConfigStore(new ConfigSnapshotFromReader(ConfigurationReader, "local"), Logger);

				ApmServerInfo = apmServerInfo ?? new ApmServerInfo();

				PayloadSender = payloadSender
					?? new PayloadSenderV2(Logger, ConfigStore.CurrentSnapshot, Service, system, ApmServerInfo,
						isEnabled: ConfigurationReader.Enabled);

				if (ConfigurationReader.Enabled)
				{
					CentralConfigFetcher = centralConfigFetcher ?? new CentralConfigFetcher(Logger, ConfigStore, Service);
					MetricsCollector = metricsCollector ?? new MetricsCollector(Logger, PayloadSender, ConfigStore);
					MetricsCollector.StartCollecting();
				}

				TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigStore,
					currentExecutionSegmentsContainer ?? new CurrentExecutionSegmentsContainer(), ApmServerInfo);

				if (!ConfigurationReader.Enabled)
					Logger?.Info()?.Log("The Elastic APM .NET Agent is disabled - the agent won't capture traces and metrics.");
			}
			catch (Exception e)
			{
				logger?.Error()?.LogException(e, "Failed initializing agent.");
			}
		}

		internal ICentralConfigFetcher CentralConfigFetcher { get; }

		internal IConfigStore ConfigStore { get; }

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		private IMetricsCollector MetricsCollector { get; }

		public IPayloadSender PayloadSender { get; }

		internal IApmServerInfo ApmServerInfo { get; }

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

			CentralConfigFetcher?.Dispose();
		}
	}
}
