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
			Sampler sampler = null,
			ICurrentExecutionSegmentHolder currentExecutionSegmentHolder = null
		) : this(
			BuildLogger(logger, logLevel),
			logLevel,
			serverUrls,
			secretToken,
			captureHeaders,
			transactionSampleRate,
			configurationReader,
			payloadSender,
			sampler,
			currentExecutionSegmentHolder) { }

		private TestAgentComponents(
			IApmLogger logger,
			string logLevel,
			string serverUrls,
			string secretToken,
			string captureHeaders,
			string transactionSampleRate,
			TestAgentConfigurationReader configurationReader,
			IPayloadSender payloadSender,
			Sampler sampler,
			ICurrentExecutionSegmentHolder currentExecutionSegmentHolder
		) : base(
			logger,
			BuildConfigurationReader(
				logger,
				configurationReader,
				logLevel,
				serverUrls,
				secretToken,
				captureHeaders,
				transactionSampleRate),
			payloadSender ?? new MockPayloadSender(),
			new FakeMetricsCollector(),
			sampler,
			currentExecutionSegmentHolder) { }

		private static LogLevel ParseWithoutLogging(string value)
		{
			if (AbstractConfigurationReader.TryParseLogLevel(value, out var level)) return level.Value;

			return ConsoleLogger.DefaultLogLevel;
		}

		private static IApmLogger BuildLogger(IApmLogger logger, string logLevel) => logger ?? new TestLogger(ParseWithoutLogging(logLevel ?? "Trace"));

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
