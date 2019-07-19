using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using FluentAssertions;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestAgentComponents : AgentComponents
	{
		public TestAgentComponents(
			string logLevel = null,
			string serverUrls = null,
			string secretToken = null,
			string captureHeaders = null,
			string transactionSampleRate = null,
			IApmLogger logger = null,
			TestAgentConfigurationReader configurationReader = null,
			IPayloadSender payloadSender = null,
			ICurrentExecutionSegmentHolder currentExecutionSegmentHolder = null
		) : base(
			logger ?? new NoopLogger(),
			BuildConfigurationReader(
				logger ?? new NoopLogger(),
				configurationReader,
				logLevel,
				serverUrls,
				secretToken,
				captureHeaders,
				transactionSampleRate),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			currentExecutionSegmentHolder) { }

		private static TestAgentConfigurationReader BuildConfigurationReader(
			IApmLogger logger,
			TestAgentConfigurationReader configurationReader,
			string logLevel,
			string serverUrls,
			string secretToken,
			string captureHeaders,
			string transactionSampleRate
		)
		{
			if (configurationReader == null)
				return new TestAgentConfigurationReader(
					logger,
					serverUrls: serverUrls,
					secretToken: secretToken,
					captureHeaders: captureHeaders,
					logLevel: logLevel,
					transactionSampleRate: transactionSampleRate
				);

			logLevel.Should().BeNull();
			serverUrls.Should().BeNull();
			secretToken.Should().BeNull();
			captureHeaders.Should().BeNull();
			transactionSampleRate.Should().BeNull();

			return configurationReader;
		}
	}
}
