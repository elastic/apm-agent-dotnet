using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Elastic.Apm.Helpers;

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
			public const int FlushIntervalInMilliseconds = 10_000; // 10 seconds
			public const int MaxBatchEventCount = 10;
			public const int MaxQueueEventCount = 1000;
			public const string MetricsInterval = "30s";
			public const double MetricsIntervalInMilliseconds = 30 * 1000;
			public const string SpanFramesMinDuration = "5ms";
			public const double SpanFramesMinDurationInMilliseconds = 5;
			public const int StackTraceLimit = 50;
			public const int TransactionMaxSpans = 500;
			public const double TransactionSampleRate = 1.0;
			public const string UnknownServiceName = "unknown";
			public const bool UseElasticTraceparentHeader = true;
			public const bool VerifyServerCert = true;
			public static readonly IReadOnlyCollection<string> DefaultExcludedNamespaces = new List<string>{"System.", "Microsoft.", "MS.", "FSharp.", "Newtonsoft.Json", "Serilog", "NLog", "Giraffe."}.AsReadOnly();
			public static readonly IReadOnlyCollection<string> DefaultApplicationNamespaces = new List<string>().AsReadOnly();

			public static List<WildcardMatcher> DisableMetrics = new List<WildcardMatcher>();

			public static List<WildcardMatcher> SanitizeFieldNames;

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
					"authorization",
					"set-cookie"
				})
					SanitizeFieldNames.Add(WildcardMatcher.ValueOf(item));
			}

			public static Uri ServerUri => new Uri($"http://localhost:{ApmServerPort}");
		}

		public static class EnvVarNames
		{
			public const string CaptureBody = Prefix + "CAPTURE_BODY";
			public const string CaptureBodyContentTypes = Prefix + "CAPTURE_BODY_CONTENT_TYPES";
			public const string CaptureHeaders = Prefix + "CAPTURE_HEADERS";
			public const string CentralConfig = Prefix + "CENTRAL_CONFIG";
			public const string DisableMetrics = Prefix + "DISABLE_METRICS";
			public const string Environment = Prefix + "ENVIRONMENT";
			public const string FlushInterval = Prefix + "FLUSH_INTERVAL";
			public const string GlobalLabels = Prefix + "GLOBAL_LABELS";
			public const string LogLevel = Prefix + "LOG_LEVEL";
			public const string MaxBatchEventCount = Prefix + "MAX_BATCH_EVENT_COUNT";
			public const string MaxQueueEventCount = Prefix + "MAX_QUEUE_EVENT_COUNT";
			public const string MetricsInterval = Prefix + "METRICS_INTERVAL";
			private const string Prefix = "ELASTIC_APM_";
			public const string SanitizeFieldNames = Prefix + "SANITIZE_FIELD_NAMES";
			public const string SecretToken = Prefix + "SECRET_TOKEN";
			public const string ApiKey = Prefix + "API_KEY";
			public const string ServerUrls = Prefix + "SERVER_URLS";
			public const string ServiceName = Prefix + "SERVICE_NAME";
			public const string ServiceNodeName = Prefix + "SERVICE_NODE_NAME";
			public const string ServiceVersion = Prefix + "SERVICE_VERSION";
			public const string SpanFramesMinDuration = Prefix + "SPAN_FRAMES_MIN_DURATION";
			public const string StackTraceLimit = Prefix + "STACK_TRACE_LIMIT";
			public const string TransactionMaxSpans = Prefix + "TRANSACTION_MAX_SPANS";
			public const string TransactionSampleRate = Prefix + "TRANSACTION_SAMPLE_RATE";
			public const string UseElasticTraceparentHeader = Prefix + "USE_ELASTIC_TRACEPARENT_HEADER";
			public const string VerifyServerCert = Prefix + "VERIFY_SERVER_CERT";
			public const string ExcludedNamespaces = Prefix + "EXCLUDED_NAMESPACES";
			public const string ApplicationNamespaces = Prefix + "APPLICATION_NAMESPACES";
		}

		public static class KeyNames
		{
			public const string CaptureBody = "ElasticApm:CaptureBody";
			public const string CaptureBodyContentTypes = "ElasticApm:CaptureBodyContentTypes";
			public const string CaptureHeaders = "ElasticApm:CaptureHeaders";
			public const string CentralConfig = "ElasticApm:CentralConfig";
			public const string DisableMetrics = "ElasticApm:DisableMetrics";
			public const string Environment = "ElasticApm:Environment";
			public const string FlushInterval = "ElasticApm:FlushInterval";
			public const string GlobalLabels = "ElasticApm:GlobalLabels";
			public const string LogLevel = "ElasticApm:LogLevel";
			public const string MaxBatchEventCount = "ElasticApm:MaxBatchEventCount";
			public const string MaxQueueEventCount = "ElasticApm:MaxQueueEventCount";
			public const string MetricsInterval = "ElasticApm:MetricsInterval";
			public const string SanitizeFieldNames = "ElasticApm:SanitizeFieldNames";
			public const string SecretToken = "ElasticApm:SecretToken";
			public const string ApiKey = "ElasticApm:ApiKey";
			public const string ServerUrls = "ElasticApm:ServerUrls";
			public const string ServiceName = "ElasticApm:ServiceName";
			public const string ServiceNodeName = "ElasticApm:ServiceNodeName";
			public const string ServiceVersion = "ElasticApm:ServiceVersion";
			public const string SpanFramesMinDuration = "ElasticApm:SpanFramesMinDuration";
			public const string StackTraceLimit = "ElasticApm:StackTraceLimit";
			public const string TransactionMaxSpans = "ElasticApm:TransactionMaxSpans";
			public const string TransactionSampleRate = "ElasticApm:TransactionSampleRate";
			public const string UseElasticTraceparentheader = "ElasticApm:UseElasticTraceparentHeder";
			public const string VerifyServerCert = "ElasticApm:VerifyServerCert";
			public const string ExcludedNamespaces = "ElasticApm:ExcludedNamespaces";
			public const string ApplicationNamespaces = "ElasticApm:ApplicationNamespaces";
		}

		public static class SupportedValues
		{
			public const string CaptureBodyAll = "all";
			public const string CaptureBodyErrors = "errors";
			public const string CaptureBodyOff = "off";
			public const string CaptureBodyTransactions = "transactions";

			public static readonly List<string> CaptureBodySupportedValues =
				new List<string> { CaptureBodyOff, CaptureBodyAll, CaptureBodyErrors, CaptureBodyTransactions };
		}
	}
}
