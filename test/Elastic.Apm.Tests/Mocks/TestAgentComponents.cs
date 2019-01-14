using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestAgentComponents : AgentComponents
	{
		public TestAgentComponents(
			string logLevel = "Debug",
			string serverUrls = null,
			Service service = null,
			IPayloadSender payloadSender = null
		)
			: this(new TestAgentConfigurationReader(
				new TestLogger(AbstractConfigurationReader.ParseLogLevel(logLevel)),
				logLevel: logLevel,
				serverUrls: serverUrls
			), service, payloadSender) { }

		public TestAgentComponents(AbstractLogger logger, string serverUrls = null, Service service = null,
			IPayloadSender payloadSender = null
		)
			: this(new TestAgentConfigurationReader(logger,
				logLevel: logger.LogLevel.ToString(),
				serverUrls: serverUrls
			), service, payloadSender) { }

		public TestAgentComponents(
			TestAgentConfigurationReader reader,
			Service service = null,
			IPayloadSender payloadSender = null
		) : base(reader.Logger, reader, service, payloadSender ?? new MockPayloadSender()) { }
	}
}
