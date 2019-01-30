using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm
{
	public class AgentComponents : IApmAgent
	{
		public AgentComponents(
			IApmLogger logger = null,
			IConfigurationReader configurationReader = null,
			Service service = null,
			IPayloadSender payloadSender = null
		)
		{
			Logger = logger ?? ConsoleLogger.Instance;
			ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(Logger);
			Service = service ?? Service.GetDefaultService(ConfigurationReader);
			PayloadSender = payloadSender ?? new PayloadSender(Logger, ConfigurationReader);
			TracerInternal = new Tracer(Logger, Service, PayloadSender);
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
		private Service Service { get; }

		public ITracer Tracer => TracerInternal;

		internal Tracer TracerInternal { get; }

		internal TransactionContainer TransactionContainer { get; }
	}
}
