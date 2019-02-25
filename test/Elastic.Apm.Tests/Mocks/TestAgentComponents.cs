using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestAgentComponents : AgentComponents
	{
		public TestAgentComponents()
			: this(new TestAgentConfigurationReader(new TestLogger(AbstractConfigurationReader.ParseLogLevel("Debug")))) { }

		public TestAgentComponents(
			string logLevel = "Debug",
			string serverUrls = null,
			string secretToken = null,
			IPayloadSender payloadSender = null
		)
			: this(new TestAgentConfigurationReader(
				new TestLogger(AbstractConfigurationReader.ParseLogLevel(logLevel)),
				serverUrls: serverUrls,
				secretToken: secretToken,
				logLevel: logLevel
			), payloadSender) { }

		public TestAgentComponents(TestLogger logger, string serverUrls = null, IPayloadSender payloadSender = null)
			: this(new TestAgentConfigurationReader(logger, serverUrls: serverUrls), payloadSender) { }

		public TestAgentComponents(
			TestAgentConfigurationReader reader,
			IPayloadSender payloadSender = null
		) : base(reader.Logger, reader, payloadSender ?? new MockPayloadSender()) { }
	}
}
