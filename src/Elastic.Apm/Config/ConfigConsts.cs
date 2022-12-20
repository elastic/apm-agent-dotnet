// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Cloud;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public static class ConfigConsts
	{
		public static class Constraints
		{
			public const double MinMetricsIntervalInMilliseconds = 1000;
		}

		public static class DefaultValues
		{
			public const int ApmServerPort = 8200;
			public const string CaptureBody = SupportedValues.CaptureBodyOff;
			public const string CaptureBodyContentTypes = "application/x-www-form-urlencoded*, text/*, application/json*, application/xml*";
			public const bool CaptureHeaders = true;
			public const bool CentralConfig = true;
			public const string CloudProvider = SupportedValues.CloudProviderAuto;
			public const bool EnableOpenTelemetryBridge = false;
			public const string ExitSpanMinDuration = "0ms";
			public const int ExitSpanMinDurationInMilliseconds = 0;
			public const int FlushIntervalInMilliseconds = 10_000; // 10 seconds
			public const LogLevel LogLevel = Logging.LogLevel.Error;
			public const int MaxBatchEventCount = 10;
			public const int MaxQueueEventCount = 1000;
			public const string MetricsInterval = "30s";
			public const double MetricsIntervalInMilliseconds = 30 * 1000;
			public const bool SpanCompressionEnabled = true;
			public const string SpanCompressionExactMatchMaxDuration = "50ms";
			public const double SpanCompressionExactMatchMaxDurationInMilliseconds = 50;
			public const string SpanCompressionSameKindMaxDuration = "0ms";
			public const double SpanCompressionSameKindMaxDurationInMilliseconds = 0;
			public const string SpanFramesMinDuration = "5ms";
			public const double SpanFramesMinDurationInMilliseconds = 5;
			public const string SpanStackTraceMinDuration = "5ms";
			public const double SpanStackTraceMinDurationInMilliseconds = 5;
			public const int StackTraceLimit = 50;
			public const bool TraceContextIgnoreSampledFalse = false;
			public const int TransactionMaxSpans = 500;
			public const double TransactionSampleRate = 1.0;
			public const string UnknownServiceName = "unknown-" + Consts.AgentName + "-service";
			public const bool UseElasticTraceparentHeader = true;
			public const bool VerifyServerCert = true;
			public const string TraceContinuationStrategy = "continue";

			public static readonly IReadOnlyCollection<string> DefaultApplicationNamespaces = new List<string>().AsReadOnly();

			public static readonly IReadOnlyCollection<string> DefaultExcludedNamespaces =
				new List<string>
				{
					"System.",
					"Microsoft.",
					"MS.",
					"FSharp.",
					"Newtonsoft.Json",
					"Serilog",
					"NLog",
					"Giraffe."
				}.AsReadOnly();

			public static List<WildcardMatcher> DisableMetrics = new List<WildcardMatcher>();

			public static List<WildcardMatcher> IgnoreMessageQueues = new List<WildcardMatcher>();

			public static List<WildcardMatcher> SanitizeFieldNames;

			public static List<WildcardMatcher> TransactionIgnoreUrls;

			static DefaultValues()
			{
				SanitizeFieldNames = new List<WildcardMatcher>();
				foreach (var item in new List<string>
						 {
							 "password",
							 "passwd",
							 "pwd",
							 "secret",
							 "*key",
							 "*token*",
							 "*session*",
							 "*credit*",
							 "*card*",
							 "*auth*",
							 "set-cookie",
							 "*principal*"
						 })
					SanitizeFieldNames.Add(WildcardMatcher.ValueOf(item));

				TransactionIgnoreUrls = new List<WildcardMatcher>();

				foreach (var item in new List<string>
						 {
							 "/VAADIN/*",
							 "/heartbeat*",
							 "/favicon.ico",
							 "*.js",
							 "*.css",
							 "*.jpg",
							 "*.jpeg",
							 "*.png",
							 "*.gif",
							 "*.webp",
							 "*.svg",
							 "*.woff",
							 "*.woff2"
						 })
					TransactionIgnoreUrls.Add(WildcardMatcher.ValueOf(item));
			}

			public static Uri ServerUri => new Uri($"http://127.0.0.1:{ApmServerPort}");
		}

		public static class EnvVarNames
		{
			public const string ApiKey = Prefix + "API_KEY";
			public const string ApplicationNamespaces = Prefix + "APPLICATION_NAMESPACES";
			public const string CaptureBody = Prefix + "CAPTURE_BODY";
			public const string CaptureBodyContentTypes = Prefix + "CAPTURE_BODY_CONTENT_TYPES";
			public const string CaptureHeaders = Prefix + "CAPTURE_HEADERS";
			public const string CentralConfig = Prefix + "CENTRAL_CONFIG";
			public const string CloudProvider = Prefix + "CLOUD_PROVIDER";
			public const string DisableMetrics = Prefix + "DISABLE_METRICS";
			public const string Enabled = Prefix + "ENABLED";
			public const string EnableOpenTelemetryBridge = Prefix + "ENABLEOPENTELEMETRYBRIDGE";
			public const string Environment = Prefix + "ENVIRONMENT";
			public const string ExcludedNamespaces = Prefix + "EXCLUDED_NAMESPACES";
			public const string ExitSpanMinDuration = Prefix + "EXIT_SPAN_MIN_DURATION";
			public const string FlushInterval = Prefix + "FLUSH_INTERVAL";

			//This setting is Full Framework only:
			public const string FullFrameworkConfigurationReaderType = Prefix + "FULL_FRAMEWORK_CONFIGURATION_READER_TYPE";
			public const string GlobalLabels = Prefix + "GLOBAL_LABELS";
			public const string HostName = Prefix + "HOSTNAME";
			public const string IgnoreMessageQueues = Prefix + "IGNORE_MESSAGE_QUEUES";
			public const string LogLevel = Prefix + "LOG_LEVEL";
			public const string MaxBatchEventCount = Prefix + "MAX_BATCH_EVENT_COUNT";
			public const string MaxQueueEventCount = Prefix + "MAX_QUEUE_EVENT_COUNT";
			public const string MetricsInterval = Prefix + "METRICS_INTERVAL";
			internal const string Prefix = "ELASTIC_APM_";
			public const string Recording = Prefix + "RECORDING";
			public const string SanitizeFieldNames = Prefix + "SANITIZE_FIELD_NAMES";
			public const string SecretToken = Prefix + "SECRET_TOKEN";
			public const string ServerCert = Prefix + "SERVER_CERT";
			public const string ServerUrl = Prefix + "SERVER_URL";
			public const string ServerUrls = Prefix + "SERVER_URLS";
			public const string UseWindowsCredentials = Prefix + "USE_WINDOWS_CREDENTIALS";
			public const string ServiceName = Prefix + "SERVICE_NAME";
			public const string ServiceNodeName = Prefix + "SERVICE_NODE_NAME";
			public const string ServiceVersion = Prefix + "SERVICE_VERSION";
			public const string SpanCompressionEnabled = Prefix + "SPAN_COMPRESSION_ENABLED";
			public const string SpanCompressionExactMatchMaxDuration = Prefix + "SPAN_COMPRESSION_EXACT_MATCH_MAX_DURATION";
			public const string SpanCompressionSameKindMaxDuration = Prefix + "SPAN_COMPRESSION_SAME_KIND_MAX_DURATION";
			public const string SpanStackTraceMinDuration = Prefix + "SPAN_STACK_TRACE_MIN_DURATION";
			public const string SpanFramesMinDuration = Prefix + "SPAN_FRAMES_MIN_DURATION";
			public const string StackTraceLimit = Prefix + "STACK_TRACE_LIMIT";
			public const string TraceContextIgnoreSampledFalse = Prefix + "TRACE_CONTEXT_IGNORE_SAMPLED_FALSE";
			public const string TraceContinuationStrategy = Prefix + "TRACE_CONTINUATION_STRATEGY";
			public const string TransactionIgnoreUrls = Prefix + "TRANSACTION_IGNORE_URLS";
			public const string TransactionMaxSpans = Prefix + "TRANSACTION_MAX_SPANS";
			public const string TransactionSampleRate = Prefix + "TRANSACTION_SAMPLE_RATE";
			public const string UseElasticTraceparentHeader = Prefix + "USE_ELASTIC_TRACEPARENT_HEADER";
			public const string VerifyServerCert = Prefix + "VERIFY_SERVER_CERT";
		}

		public static class KeyNames
		{
			public const string ApiKey = Prefix + nameof(ApiKey);
			public const string ApplicationNamespaces = Prefix + nameof(ApplicationNamespaces);
			public const string CaptureBody = Prefix + nameof(CaptureBody);
			public const string CaptureBodyContentTypes = Prefix + nameof(CaptureBodyContentTypes);
			public const string CaptureHeaders = Prefix + nameof(CaptureHeaders);
			public const string CentralConfig = Prefix + nameof(CentralConfig);
			public const string CloudProvider = Prefix + nameof(CloudProvider);
			public const string DisableMetrics = Prefix + nameof(DisableMetrics);
			public const string Enabled = Prefix + nameof(Enabled);
			public const string EnableOpenTelemetryBridge = Prefix + nameof(EnableOpenTelemetryBridge);
			public const string Environment = Prefix + nameof(Environment);
			public const string ExcludedNamespaces = Prefix + nameof(ExcludedNamespaces);
			public const string ExitSpanMinDuration = Prefix + nameof(ExitSpanMinDuration);
			public const string FlushInterval = Prefix + nameof(FlushInterval);

			//This setting is Full Framework only:
			public const string FullFrameworkConfigurationReaderType = Prefix + nameof(FullFrameworkConfigurationReaderType);
			public const string GlobalLabels = Prefix + nameof(GlobalLabels);
			public const string HostName = Prefix + nameof(HostName);
			public const string IgnoreMessageQueues = Prefix + nameof(IgnoreMessageQueues);
			public const string LogLevel = Prefix + nameof(LogLevel);
			public const string MaxBatchEventCount = Prefix + nameof(MaxBatchEventCount);
			public const string MaxQueueEventCount = Prefix + nameof(MaxQueueEventCount);
			public const string MetricsInterval = Prefix + nameof(MetricsInterval);
			internal const string Prefix = "ElasticApm:";
			public const string Recording = Prefix + nameof(Recording);
			public const string SanitizeFieldNames = Prefix + nameof(SanitizeFieldNames);
			public const string SecretToken = Prefix + nameof(SecretToken);
			public const string ServerCert = Prefix + nameof(ServerCert);
			public const string ServerUrl = Prefix + nameof(ServerUrl);
			public const string ServerUrls = Prefix + nameof(ServerUrls);
			public const string UseWindowsCredentials = Prefix + nameof(UseWindowsCredentials);
			public const string ServiceName = Prefix + nameof(ServiceName);
			public const string ServiceNodeName = Prefix + nameof(ServiceNodeName);
			public const string ServiceVersion = Prefix + nameof(ServiceVersion);
			public const string SpanCompressionEnabled = Prefix + nameof(SpanCompressionEnabled);
			public const string SpanCompressionExactMatchMaxDuration = Prefix + nameof(SpanCompressionExactMatchMaxDuration);
			public const string SpanCompressionSameKindMaxDuration = Prefix + nameof(SpanCompressionSameKindMaxDuration);
			public const string SpanStackTraceMinDuration = Prefix + nameof(SpanStackTraceMinDuration);
			public const string SpanFramesMinDuration = Prefix + nameof(SpanFramesMinDuration);
			public const string StackTraceLimit = Prefix + nameof(StackTraceLimit);
			public const string TraceContextIgnoreSampledFalse = Prefix + nameof(TraceContextIgnoreSampledFalse);
			public const string TraceContinuationStrategy = Prefix + nameof(TraceContinuationStrategy);
			public const string TransactionIgnoreUrls = Prefix + nameof(TransactionIgnoreUrls);
			public const string TransactionMaxSpans = Prefix + nameof(TransactionMaxSpans);
			public const string TransactionSampleRate = Prefix + nameof(TransactionSampleRate);
			public const string UseElasticTraceparentHeader = Prefix + nameof(UseElasticTraceparentHeader);
			public const string VerifyServerCert = Prefix + nameof(VerifyServerCert);
		}

		public static class SupportedValues
		{
			public const string CaptureBodyAll = "all";
			public const string CaptureBodyErrors = "errors";
			public const string CaptureBodyOff = "off";
			public const string CaptureBodyTransactions = "transactions";
			public const string CloudProviderAuto = "auto";

			public const string Continue = "continue";
			public const string Restart = "restart";
			public const string RestartExternal = "restart_external";

			public const string CloudProviderAws = AwsCloudMetadataProvider.Name;
			public const string CloudProviderAzure = AzureCloudMetadataProvider.Name;
			public const string CloudProviderGcp = GcpCloudMetadataProvider.Name;
			public const string CloudProviderNone = "none";

			public static readonly List<string> CaptureBodySupportedValues = new() { CaptureBodyOff, CaptureBodyAll, CaptureBodyErrors, CaptureBodyTransactions };

			public static readonly List<string> TraceContinuationStrategySupportedValues = new() { Continue, Restart, RestartExternal };

			public static readonly HashSet<string> CloudProviders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				CloudProviderAuto,
				CloudProviderAws,
				CloudProviderAzure,
				CloudProviderGcp,
				CloudProviderNone
			};
		}
	}
}
