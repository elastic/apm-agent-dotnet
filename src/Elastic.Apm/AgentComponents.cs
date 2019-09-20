using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Report;

namespace Elastic.Apm
{
	public class AgentComponents : IApmAgent, IDisposable
	{
		public AgentComponents(
			IApmLogger logger = null,
			IConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null,
			[CallerMemberName] string dbgName = null
		) : this(dbgName, logger, configurationReader, payloadSender, null, null, null) { }

		internal AgentComponents(
			string dbgName,
			IApmLogger logger,
			IConfigurationReader configurationReader,
			IPayloadSender payloadSender,
			IMetricsCollector metricsCollector,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			ICentralConfigFetcher centralConfigFetcher
		)
		{
			var tempLogger = logger ?? ConsoleLogger.LoggerOrDefault(configurationReader?.LogLevel);
			ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(tempLogger);
			Logger = logger ?? ConsoleLogger.LoggerOrDefault(ConfigurationReader.LogLevel);
			Service = Service.GetDefaultService(ConfigurationReader, Logger);

			var systemInfoHelper = new SystemInfoHelper(Logger);
			var system = systemInfoHelper.ReadContainerId(Logger);

			ConfigStore = new ConfigStore(new ConfigSnapshotFromReader(ConfigurationReader, "local"), Logger);

			PayloadSender = payloadSender ?? new PayloadSenderV2(Logger, ConfigStore.CurrentSnapshot, Service, system, dbgName: dbgName);

			MetricsCollector = metricsCollector ?? new MetricsCollector(Logger, PayloadSender, ConfigurationReader);
			MetricsCollector.StartCollecting();

			CentralConfigFetcher = centralConfigFetcher ?? new CentralConfigFetcher(Logger, ConfigStore, Service);

			TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigStore,
				currentExecutionSegmentsContainer ?? new CurrentExecutionSegmentsContainer(Logger));
		}

		private ICentralConfigFetcher CentralConfigFetcher { get; }

		internal IConfigStore ConfigStore { get; }

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		private IMetricsCollector MetricsCollector { get; }

		public IPayloadSender PayloadSender { get; }

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

			CentralConfigFetcher.Dispose();
		}
	}
}
