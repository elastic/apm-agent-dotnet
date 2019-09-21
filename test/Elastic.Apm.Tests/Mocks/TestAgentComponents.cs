using System.Runtime.CompilerServices;
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
			MockConfigSnapshot config = null,
			IPayloadSender payloadSender = null,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer = null,
			string captureBody = ConfigConsts.SupportedValues.CaptureBodyOff,
			string captureBodyContentTypes = ConfigConsts.DefaultValues.CaptureBodyContentTypes,
			ICentralConfigFetcher centralConfigFetcher = null,
			bool useRealCentralConfigFetcher = false,
			[CallerMemberName] string dbgName = null
		) : base(
			dbgName,
			logger ?? new NoopLogger(),
			config ?? new MockConfigSnapshot(
				logger ?? new NoopLogger(),
				captureBody: captureBody,
				captureBodyContentTypes: captureBodyContentTypes),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			currentExecutionSegmentsContainer,
			useRealCentralConfigFetcher ? null : centralConfigFetcher ?? new NoopCentralConfigFetcher()
		) { }
	}
}
