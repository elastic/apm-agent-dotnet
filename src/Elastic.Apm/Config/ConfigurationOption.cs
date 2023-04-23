// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using static Elastic.Apm.Config.ConfigurationOption;

namespace Elastic.Apm.Config
{
	public enum ConfigurationOption
	{
		ApiKey,
		ApplicationNamespaces,
		CaptureBody,
		CaptureBodyContentTypes,
		CaptureHeaders,
		CentralConfig,
		CloudProvider,
		DisableMetrics,
		Enabled,
		OpenTelemetryBridgeEnabled,
		Environment,
		ExcludedNamespaces,
		ExitSpanMinDuration,
		FlushInterval,
		GlobalLabels,
		HostName,
		IgnoreMessageQueues,
		LogLevel,
		MaxBatchEventCount,
		MaxQueueEventCount,
		MetricsInterval,
		Recording,
		SanitizeFieldNames,
		SecretToken,
		ServerCert,
		ServerUrl,
		UseWindowsCredentials,
		ServiceName,
		ServiceNodeName,
		ServiceVersion,
		SpanCompressionEnabled,
		SpanCompressionExactMatchMaxDuration,
		SpanCompressionSameKindMaxDuration,
		SpanStackTraceMinDuration,
		StackTraceLimit,
		TraceContinuationStrategy,
		TransactionIgnoreUrls,
		TransactionMaxSpans,
		TransactionSampleRate,
		UseElasticTraceparentHeader,
		VerifyServerCert,
		ServerUrls,
		SpanFramesMinDuration,
		TraceContextIgnoreSampledFalse,
#if NET46_OR_GREATER
		FullFrameworkConfigurationReaderType,
#endif
	}

	public enum ConfigurationOrigin { Environment, Application, CentralConfig }

	internal static class ConfigurationOptionExtensions
	{
		internal const string EnvPrefix = "ELASTIC_APM_";
		internal const string KeyPrefix = "ElasticApm:";

		private static readonly IReadOnlyCollection<ConfigurationOption> All =
			(ConfigurationOption[])Enum.GetValues(typeof(ConfigurationOption));

		public static IReadOnlyCollection<ConfigurationOption> AllOptions() => All;

		public static string ToNormalizedName(this ConfigurationOption option) =>
			option.ToEnvironmentVariable().Substring(EnvPrefix.Length).ToLower();

		public static string ToNamedString(this ConfigurationOption option) =>
			option.ToConfigKey().Substring(KeyPrefix.Length);

		public static string ToConfigurationName(this ConfigurationOption option, ConfigurationOrigin origin) =>
			origin switch
			{
				ConfigurationOrigin.Environment => option.ToEnvironmentVariable(),
				ConfigurationOrigin.Application => option.ToConfigKey(),
				ConfigurationOrigin.CentralConfig => option.ToConfigKey(),
				_ => throw new System.ArgumentOutOfRangeException(nameof(origin), origin, null)
			};

		public static string ToEnvironmentVariable(this ConfigurationOption option) =>
			option switch
			{
				ApiKey => EnvPrefix + "API_KEY",
				ApplicationNamespaces => EnvPrefix + "APPLICATION_NAMESPACES",
				CaptureBody => EnvPrefix + "CAPTURE_BODY",
				CaptureBodyContentTypes => EnvPrefix + "CAPTURE_BODY_CONTENT_TYPES",
				CaptureHeaders => EnvPrefix + "CAPTURE_HEADERS",
				CentralConfig => EnvPrefix + "CENTRAL_CONFIG",
				CloudProvider => EnvPrefix + "CLOUD_PROVIDER",
				DisableMetrics => EnvPrefix + "DISABLE_METRICS",
				Enabled => EnvPrefix + "ENABLED",
				OpenTelemetryBridgeEnabled => EnvPrefix + "OPENTELEMETRY_BRIDGE_ENABLED",
				ConfigurationOption.Environment => EnvPrefix + "ENVIRONMENT",
				ExcludedNamespaces => EnvPrefix + "EXCLUDED_NAMESPACES",
				ExitSpanMinDuration => EnvPrefix + "EXIT_SPAN_MIN_DURATION",
				FlushInterval => EnvPrefix + "FLUSH_INTERVAL",
				GlobalLabels => EnvPrefix + "GLOBAL_LABELS",
				HostName => EnvPrefix + "HOSTNAME",
				IgnoreMessageQueues => EnvPrefix + "IGNORE_MESSAGE_QUEUES",
				LogLevel => EnvPrefix + "LOG_LEVEL",
				MaxBatchEventCount => EnvPrefix + "MAX_BATCH_EVENT_COUNT",
				MaxQueueEventCount => EnvPrefix + "MAX_QUEUE_EVENT_COUNT",
				MetricsInterval => EnvPrefix + "METRICS_INTERVAL",
				Recording => EnvPrefix + "RECORDING",
				SanitizeFieldNames => EnvPrefix + "SANITIZE_FIELD_NAMES",
				SecretToken => EnvPrefix + "SECRET_TOKEN",
				ServerCert => EnvPrefix + "SERVER_CERT",
				ServerUrl => EnvPrefix + "SERVER_URL",
				ServerUrls => EnvPrefix + "SERVER_URLS",
				UseWindowsCredentials => EnvPrefix + "USE_WINDOWS_CREDENTIALS",
				ServiceName => EnvPrefix + "SERVICE_NAME",
				ServiceNodeName => EnvPrefix + "SERVICE_NODE_NAME",
				ServiceVersion => EnvPrefix + "SERVICE_VERSION",
				SpanCompressionEnabled => EnvPrefix + "SPAN_COMPRESSION_ENABLED",
				SpanCompressionExactMatchMaxDuration => EnvPrefix + "SPAN_COMPRESSION_EXACT_MATCH_MAX_DURATION",
				SpanCompressionSameKindMaxDuration => EnvPrefix + "SPAN_COMPRESSION_SAME_KIND_MAX_DURATION",
				SpanStackTraceMinDuration => EnvPrefix + "SPAN_STACK_TRACE_MIN_DURATION",
				SpanFramesMinDuration => EnvPrefix + "SPAN_FRAMES_MIN_DURATION",
				StackTraceLimit => EnvPrefix + "STACK_TRACE_LIMIT",
				TraceContextIgnoreSampledFalse => EnvPrefix + "TRACE_CONTEXT_IGNORE_SAMPLED_FALSE",
				TraceContinuationStrategy => EnvPrefix + "TRACE_CONTINUATION_STRATEGY",
				TransactionIgnoreUrls => EnvPrefix + "TRANSACTION_IGNORE_URLS",
				TransactionMaxSpans => EnvPrefix + "TRANSACTION_MAX_SPANS",
				TransactionSampleRate => EnvPrefix + "TRANSACTION_SAMPLE_RATE",
				UseElasticTraceparentHeader => EnvPrefix + "USE_ELASTIC_TRACEPARENT_HEADER",
				VerifyServerCert => EnvPrefix + "VERIFY_SERVER_CERT",
#if NET46_OR_GREATER
				FullFrameworkConfigurationReaderType => EnvPrefix + "FULL_FRAMEWORK_CONFIGURATION_READER_TYPE",
#endif
				_ => throw new System.ArgumentOutOfRangeException(nameof(option), option, null)
			};

		public static string ToConfigKey(this ConfigurationOption option) =>
			option switch
			{
				ApiKey => KeyPrefix + nameof(ApiKey),
				ApplicationNamespaces => KeyPrefix + nameof(ApplicationNamespaces),
				CaptureBody => KeyPrefix + nameof(CaptureBody),
				CaptureBodyContentTypes => KeyPrefix + nameof(CaptureBodyContentTypes),
				CaptureHeaders => KeyPrefix + nameof(CaptureHeaders),
				CentralConfig => KeyPrefix + nameof(CentralConfig),
				CloudProvider => KeyPrefix + nameof(CloudProvider),
				DisableMetrics => KeyPrefix + nameof(DisableMetrics),
				Enabled => KeyPrefix + nameof(Enabled),
				OpenTelemetryBridgeEnabled => KeyPrefix + nameof(OpenTelemetryBridgeEnabled),
				ConfigurationOption.Environment => KeyPrefix + nameof(ConfigurationOption.Environment),
				ExcludedNamespaces => KeyPrefix + nameof(ExcludedNamespaces),
				ExitSpanMinDuration => KeyPrefix + nameof(ExitSpanMinDuration),
				FlushInterval => KeyPrefix + nameof(FlushInterval),
				GlobalLabels => KeyPrefix + nameof(GlobalLabels),
				HostName => KeyPrefix + nameof(HostName),
				IgnoreMessageQueues => KeyPrefix + nameof(IgnoreMessageQueues),
				LogLevel => KeyPrefix + nameof(ConfigurationOption.LogLevel),
				MaxBatchEventCount => KeyPrefix + nameof(MaxBatchEventCount),
				MaxQueueEventCount => KeyPrefix + nameof(MaxQueueEventCount),
				MetricsInterval => KeyPrefix + nameof(MetricsInterval),
				Recording => KeyPrefix + nameof(Recording),
				SanitizeFieldNames => KeyPrefix + nameof(SanitizeFieldNames),
				SecretToken => KeyPrefix + nameof(SecretToken),
				ServerCert => KeyPrefix + nameof(ServerCert),
				ServerUrl => KeyPrefix + nameof(ServerUrl),
				UseWindowsCredentials => KeyPrefix + nameof(UseWindowsCredentials),
				ServiceName => KeyPrefix + nameof(ServiceName),
				ServiceNodeName => KeyPrefix + nameof(ServiceNodeName),
				ServiceVersion => KeyPrefix + nameof(ServiceVersion),
				SpanCompressionEnabled => KeyPrefix + nameof(SpanCompressionEnabled),
				SpanCompressionExactMatchMaxDuration => KeyPrefix + nameof(SpanCompressionExactMatchMaxDuration),
				SpanCompressionSameKindMaxDuration => KeyPrefix + nameof(SpanCompressionSameKindMaxDuration),
				SpanStackTraceMinDuration => KeyPrefix + nameof(SpanStackTraceMinDuration),
				StackTraceLimit => KeyPrefix + nameof(StackTraceLimit),
				TraceContinuationStrategy => KeyPrefix + nameof(TraceContinuationStrategy),
				TransactionIgnoreUrls => KeyPrefix + nameof(TransactionIgnoreUrls),
				TransactionMaxSpans => KeyPrefix + nameof(TransactionMaxSpans),
				TransactionSampleRate => KeyPrefix + nameof(TransactionSampleRate),
				UseElasticTraceparentHeader => KeyPrefix + nameof(UseElasticTraceparentHeader),
				VerifyServerCert => KeyPrefix + nameof(VerifyServerCert),
				ServerUrls => KeyPrefix + nameof(ServerUrls),
				SpanFramesMinDuration => KeyPrefix + nameof(SpanFramesMinDuration),
				TraceContextIgnoreSampledFalse => KeyPrefix + nameof(TraceContextIgnoreSampledFalse),
#if NET46_OR_GREATER
				FullFrameworkConfigurationReaderType => KeyPrefix + nameof(FullFrameworkConfigurationReaderType),
#endif
				_ => throw new System.ArgumentOutOfRangeException(nameof(option), option, null)
			};
	}
}
