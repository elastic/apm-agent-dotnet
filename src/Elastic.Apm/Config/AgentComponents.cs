using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Config
{
	public class AgentComponents : IApmAgent
	{
		public AgentComponents(
			AbstractLogger logger,
			IConfigurationReader configurationReader = null,
			Service service = null,
			IPayloadSender payloadSender = null)
		{
			Service = service ?? Service.Default;
			Logger = logger ?? ConsoleLogger.Instance;
			ConfigurationReader = configurationReader ?? new EnvironmentConfigurationReader(Logger);
			PayloadSender = payloadSender ?? new PayloadSender(Logger, ConfigurationReader);
			Tracer = new Tracer(Logger, Service, PayloadSender);
		}

		public AbstractLogger Logger { get; }

		public IPayloadSender PayloadSender { get; }

		public IConfigurationReader ConfigurationReader { get; }

		public ITracer Tracer { get; }

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		public Service Service { get; }
	}
}
