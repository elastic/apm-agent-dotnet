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
		private readonly Func<string, string> _reader;
		public string Description => MockConfiguration.Origin;

		public MockConfigurationEnvironmentProvider(Func<string, string> reader) => _reader = reader;

		public ConfigurationKeyValue Read(string variable) => new(variable, _reader(variable)?.Trim(), Description);
	}

	internal class MockConfiguration : FallbackConfigurationBase, IConfiguration
	{
		public const string Origin = "unit test configuration";
		private const string ThisClassName = nameof(MockConfiguration);

		public MockConfiguration(IApmLogger logger = null,
			string logLevel = null,
			string serverUrls = null,
			string serviceName = null,
			string serviceVersion = null,
			string environment = null,
			string serviceNodeName = null,
			string secretToken = null,
			string apiKey = null,
			string captureHeaders = null,
			string centralConfig = null,
			string description = null,
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
			string traceContinuationStrategy = null
		) : base(
			logger,
			new ConfigurationDefaults { DebugName = ThisClassName },
			new NullConfigurationKeyValueProvider(),
			new MockConfigurationEnvironmentProvider(key => key switch
			{
				EnvVarNames.ApiKey => apiKey,
				EnvVarNames.ApplicationNamespaces => applicationNamespaces,
				EnvVarNames.CaptureBody => captureBody,
				EnvVarNames.CaptureBodyContentTypes => captureBodyContentTypes,
				EnvVarNames.CaptureHeaders => captureHeaders,
				EnvVarNames.CentralConfig => centralConfig,
				EnvVarNames.CloudProvider => cloudProvider,
				EnvVarNames.DisableMetrics => disableMetrics,
				EnvVarNames.Enabled => enabled,
				EnvVarNames.OpenTelemetryBridgeEnabled => openTelemetryBridgeEnabled,
				EnvVarNames.Environment => environment,
				EnvVarNames.ExcludedNamespaces => excludedNamespaces,
				EnvVarNames.ExitSpanMinDuration => exitSpanMinDuration,
				EnvVarNames.FlushInterval => flushInterval,
				EnvVarNames.GlobalLabels => globalLabels,
				EnvVarNames.HostName => hostName,
				EnvVarNames.IgnoreMessageQueues => ignoreMessageQueues,
				EnvVarNames.LogLevel => logLevel,
				EnvVarNames.MaxBatchEventCount => maxBatchEventCount,
				EnvVarNames.MaxQueueEventCount => maxQueueEventCount,
				EnvVarNames.MetricsInterval => metricsInterval,
				EnvVarNames.Recording => recording,
				EnvVarNames.SanitizeFieldNames => sanitizeFieldNames,
				EnvVarNames.SecretToken => secretToken,
				EnvVarNames.ServerCert => serverCert,
				EnvVarNames.ServerUrl => serverUrl,
				EnvVarNames.ServerUrls => serverUrls,
				EnvVarNames.UseWindowsCredentials => useWindowsCredentials,
				EnvVarNames.ServiceName => serviceName,
				EnvVarNames.ServiceNodeName => serviceNodeName,
				EnvVarNames.ServiceVersion => serviceVersion,
				EnvVarNames.SpanCompressionEnabled => spanCompressionEnabled,
				EnvVarNames.SpanCompressionExactMatchMaxDuration => spanCompressionExactMatchMaxDuration,
				EnvVarNames.SpanCompressionSameKindMaxDuration => spanCompressionSameKindMaxDuration,
				EnvVarNames.SpanStackTraceMinDuration => spanStackTraceMinDurationInMilliseconds,
				EnvVarNames.SpanFramesMinDuration => spanFramesMinDurationInMilliseconds,
				EnvVarNames.StackTraceLimit => stackTraceLimit,
				EnvVarNames.TraceContextIgnoreSampledFalse => traceContextIgnoreSampledFalse,
				EnvVarNames.TraceContinuationStrategy => traceContinuationStrategy,
				EnvVarNames.TransactionIgnoreUrls => transactionIgnoreUrls,
				EnvVarNames.TransactionMaxSpans => transactionMaxSpans,
				EnvVarNames.TransactionSampleRate => transactionSampleRate,
				EnvVarNames.UseElasticTraceparentHeader => useElasticTraceparentHeader,
				EnvVarNames.VerifyServerCert => verifyServerCert,
				_ => throw new Exception($"{nameof(MockConfiguration)} does not have implementation for configuration : {key}")
			}),
			description
		)
		{ }
	}
}
