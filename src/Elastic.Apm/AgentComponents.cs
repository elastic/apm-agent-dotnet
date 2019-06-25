using System;
using System.IO;
using Elastic.Apm.Api;
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
			IPayloadSender payloadSender = null
		)
		{
			Logger = logger ?? ConsoleLogger.LoggerOrDefault(configurationReader?.LogLevel);
			ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(Logger);
			Service = Service.GetDefaultService(ConfigurationReader, Logger);

			var systemInfoHelper = new SystemInfoHelper(Logger);
			var system = systemInfoHelper.ReadContainerId(Logger);

			PayloadSender = payloadSender ?? new PayloadSenderV2(Logger, ConfigurationReader, Service, system);

			MetricsCollector = new MetricsCollector(Logger, PayloadSender, ConfigurationReader);
			MetricsCollector.StartCollecting();

			TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigurationReader);
			TransactionContainer = new TransactionContainer();
		}

		internal AgentComponents(
			IMetricsCollector metricsCollector,
			IApmLogger logger = null,
			IConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null
		) : this(logger, configurationReader, payloadSender)
			=> (MetricsCollector = metricsCollector ?? new MetricsCollector(Logger, PayloadSender, ConfigurationReader)).StartCollecting();

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		public IPayloadSender PayloadSender { get; }

		private IMetricsCollector MetricsCollector { get; }

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		public Service Service { get; }

		public ITracer Tracer => TracerInternal;

		private Tracer TracerInternal { get; }

		internal TransactionContainer TransactionContainer { get; }

		public void Dispose()
		{
			if (MetricsCollector is IDisposable disposableMetricsCollector)
			{
				disposableMetricsCollector.Dispose();
			}

			if (PayloadSender is IDisposable disposablePayloadSender)
			{
				disposablePayloadSender.Dispose();
			}
		}
	}
}
