using Elastic.Apm.BackendComm;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestAgentComponents : AgentComponents
	{
		public TestAgentComponents(
			IApmLogger logger = null,
			IConfigSnapshot config = null,
			IPayloadSender payloadSender = null,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer = null,
			ICentralConfigFetcher centralConfigFetcher = null
		) : base(
			logger ?? new NoopLogger(),
			config ?? new MockConfigSnapshot(logger ?? new NoopLogger()),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			currentExecutionSegmentsContainer,
			centralConfigFetcher ?? new NoopCentralConfigFetcher()
		)
		{ }
	}
}
