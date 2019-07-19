using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using FluentAssertions;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestAgentComponents : AgentComponents
	{
		public TestAgentComponents(
			IApmLogger logger = null,
			TestAgentConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null,
			ICurrentExecutionSegmentHolder currentExecutionSegmentHolder = null
		) : base(
			logger ?? new NoopLogger(),
			configurationReader ?? new TestAgentConfigurationReader(logger ?? new NoopLogger()),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			currentExecutionSegmentHolder) { }
	}
}
