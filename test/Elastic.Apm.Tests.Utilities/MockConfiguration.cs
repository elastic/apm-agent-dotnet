// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Tests.Utilities
{
	public class MockConfigurationEnvironmentProvider : IConfigurationEnvironmentValueProvider
	{
		private readonly Func<ConfigurationOption, string> _reader;
		public string Description => nameof(MockConfigurationEnvironmentProvider);

		public MockConfigurationEnvironmentProvider(Func<ConfigurationOption, string> reader) => _reader = reader;

		public EnvironmentKeyValue Read(ConfigurationOption option) => new(option, _reader(option)?.Trim(), Description);
	}

	internal class MockConfiguration : FallbackConfigurationBase, IConfiguration
	{
		public MockConfiguration(IApmLogger logger = null,
			string logLevel = null,
			string serverUrls = null,
			string serviceName = null,
			string serviceVersion = null,
			string environment = null,
			string serviceNodeName = null,
			string secretToken = null,
			string apiKey = null,
			string baggageToAttach = null,
			string captureHeaders = null,
			string centralConfig = null,
			string openTelemetryBridgeEnabled = null,
			string exitSpanMinDuration = null,
			string transactionSampleRate = null,
			string transactionMaxSpans = null,
			string metricsInterval = null,
			string captureBody = SupportedValues.CaptureBodyOff,
			string stackTraceLimit = null,
			string spanStackTraceMinDurationInMilliseconds = null,
			string spanFramesMinDurationInMilliseconds = null,
			string captureBodyContentTypes = DefaultValues.CaptureBodyContentTypes,
			string flushInterval = null,
			string maxBatchEventCount = null,
			string maxQueueEventCount = null,
			string sanitizeFieldNames = null,
			string globalLabels = null,
			string disableMetrics = null,
			string verifyServerCert = null,
			string useElasticTraceparentHeader = null,
			string applicationNamespaces = null,
			string excludedNamespaces = null,
			string transactionIgnoreUrls = null,
			string hostName = null,
			// none is **not** the default value, but we don't want to query for cloud metadata in all tests
			string cloudProvider = SupportedValues.CloudProviderNone,
			string enabled = null,
			string recording = null,
			string serverUrl = null,
			string useWindowsCredentials = null,
			string serverCert = null,
			string ignoreMessageQueues = null,
			string traceContextIgnoreSampledFalse = null,
			string spanCompressionEnabled = null,
			string spanCompressionExactMatchMaxDuration = null,
			string spanCompressionSameKindMaxDuration = null,
			string traceContinuationStrategy = null,
			string transactionNameGroups = null,
			string usePathAsTransactionName = null
		) : base(
			logger,
			new ConfigurationDefaults { DebugName = nameof(MockConfiguration) },
			new NullConfigurationKeyValueProvider(),
			new MockConfigurationEnvironmentProvider(key => key switch
			{
				ConfigurationOption.ApiKey => apiKey,
				ConfigurationOption.ApplicationNamespaces => applicationNamespaces,
				ConfigurationOption.BaggageToAttach => baggageToAttach,
				ConfigurationOption.CaptureBody => captureBody,
				ConfigurationOption.CaptureBodyContentTypes => captureBodyContentTypes,
				ConfigurationOption.CaptureHeaders => captureHeaders,
				ConfigurationOption.CentralConfig => centralConfig,
				ConfigurationOption.CloudProvider => cloudProvider,
				ConfigurationOption.DisableMetrics => disableMetrics,
				ConfigurationOption.Enabled => enabled,
				ConfigurationOption.OpenTelemetryBridgeEnabled => openTelemetryBridgeEnabled,
				ConfigurationOption.Environment => environment,
				ConfigurationOption.ExcludedNamespaces => excludedNamespaces,
				ConfigurationOption.ExitSpanMinDuration => exitSpanMinDuration,
				ConfigurationOption.FlushInterval => flushInterval,
				ConfigurationOption.GlobalLabels => globalLabels,
				ConfigurationOption.HostName => hostName,
				ConfigurationOption.IgnoreMessageQueues => ignoreMessageQueues,
				ConfigurationOption.LogLevel => logLevel,
				ConfigurationOption.MaxBatchEventCount => maxBatchEventCount,
				ConfigurationOption.MaxQueueEventCount => maxQueueEventCount,
				ConfigurationOption.MetricsInterval => metricsInterval,
				ConfigurationOption.Recording => recording,
				ConfigurationOption.SanitizeFieldNames => sanitizeFieldNames,
				ConfigurationOption.SecretToken => secretToken,
				ConfigurationOption.ServerCert => serverCert,
				ConfigurationOption.ServerUrl => serverUrl,
				ConfigurationOption.ServerUrls => serverUrls,
				ConfigurationOption.UseWindowsCredentials => useWindowsCredentials,
				ConfigurationOption.ServiceName => serviceName,
				ConfigurationOption.ServiceNodeName => serviceNodeName,
				ConfigurationOption.ServiceVersion => serviceVersion,
				ConfigurationOption.SpanCompressionEnabled => spanCompressionEnabled,
				ConfigurationOption.SpanCompressionExactMatchMaxDuration => spanCompressionExactMatchMaxDuration,
				ConfigurationOption.SpanCompressionSameKindMaxDuration => spanCompressionSameKindMaxDuration,
				ConfigurationOption.SpanStackTraceMinDuration => spanStackTraceMinDurationInMilliseconds,
				ConfigurationOption.SpanFramesMinDuration => spanFramesMinDurationInMilliseconds,
				ConfigurationOption.StackTraceLimit => stackTraceLimit,
				ConfigurationOption.TraceContextIgnoreSampledFalse => traceContextIgnoreSampledFalse,
				ConfigurationOption.TraceContinuationStrategy => traceContinuationStrategy,
				ConfigurationOption.TransactionIgnoreUrls => transactionIgnoreUrls,
				ConfigurationOption.TransactionNameGroups => transactionNameGroups,
				ConfigurationOption.TransactionMaxSpans => transactionMaxSpans,
				ConfigurationOption.TransactionSampleRate => transactionSampleRate,
				ConfigurationOption.UseElasticTraceparentHeader => useElasticTraceparentHeader,
				ConfigurationOption.UsePathAsTransactionName => usePathAsTransactionName,
				ConfigurationOption.VerifyServerCert => verifyServerCert,
				ConfigurationOption.FullFrameworkConfigurationReaderType => null,
				_ => throw new Exception($"{nameof(MockConfiguration)} does not have implementation for configuration : {key}")
			})
		)
		{ }
	}
}
