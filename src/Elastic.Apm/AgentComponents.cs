using System;
using System.IO;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
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

			Service = Service.GetDefaultService(ConfigurationReader);

			var systemInfoHelper = new SystemInfoHelper();
			var system = systemInfoHelper.ReadContainerId(Logger);

			PayloadSender = payloadSender ?? new PayloadSenderV2(Logger, ConfigurationReader, Service, system);
			TracerInternal = new Tracer(Logger, Service, PayloadSender, ConfigurationReader);
			TransactionContainer = new TransactionContainer();
		}

		public IConfigurationReader ConfigurationReader { get; }

		public IApmLogger Logger { get; }

		public IPayloadSender PayloadSender { get; }

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
			if (PayloadSender is IDisposable disposable) disposable.Dispose();
		}
	}
}
