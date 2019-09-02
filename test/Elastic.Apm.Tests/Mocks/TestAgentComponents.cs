using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestAgentComponents : AgentComponents
	{
		public TestAgentComponents(
			IApmLogger logger = null,
			TestAgentConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer = null,
			string captureBody = ConfigConsts.SupportedValues.CaptureBodyOff,
			string captureBodyContentTypes = ConfigConsts.DefaultValues.CaptureBodyContentTypes
		) : base(
			logger ?? new NoopLogger(),
			configurationReader ?? new TestAgentConfigurationReader(
				logger ?? new NoopLogger(),
				captureBody: captureBody,
				captureBodyContentTypes: captureBodyContentTypes),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			currentExecutionSegmentsContainer
			)
		{ }

	}
}
