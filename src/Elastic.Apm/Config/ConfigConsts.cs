using System;
using System.Collections.Generic;

namespace Elastic.Apm.Config
{
	public static class ConfigConsts
	{
		public static class Constraints
		{
			public const string LogLevel = "ELASTIC_APM_LOG_LEVEL";
			public const string ServerUrls = "ELASTIC_APM_SERVER_URLS";
			public const string ServiceName = "ELASTIC_APM_SERVICE_NAME";
			public const string SecretToken = "ELASTIC_APM_SECRET_TOKEN";
			public const string CaptureHeaders = "ELASTIC_APM_CAPTURE_HEADERS";
			public const string TransactionSampleRate = "ELASTIC_APM_TRANSACTION_SAMPLE_RATE";
			public const string MetricsInterval = "ELASTIC_APM_METRICS_INTERVAL";
			public const string CaptureBody = "ELASTIC_APM_CAPTURE_BODY";
			public const double MinMetricsIntervalInMilliseconds = 1000;
			public const string CaptureBodyContentTypes = "ELASTIC_APM_CAPTURE_BODY_CONTENT_TYPES";
		}

		public static class DefaultValues
		{
			public const int ApmServerPort = 8200;
			public const string MetricsInterval = "30s";
			public const double MetricsIntervalInMilliseconds = 30 * 1000;
			public const string SpanFramesMinDuration = "5ms";
			public const double SpanFramesMinDurationInMilliseconds = 5;
			public const int StackTraceLimit = 50;
			public const double TransactionSampleRate = 1.0;
			public const string UnknownServiceName = "unknown";
			public static Uri ServerUri => new Uri($"http://localhost:{ApmServerPort}");
			public const string CaptureBody = SupportedValues.CaptureBodyOff;
			public const string CaptureBodyContentTypes = "application/x-www-form-urlencoded*, text/*, application/json*, application/xml*";
		}

		public static class EnvVarNames
		{
			public const string CaptureHeaders = "ELASTIC_APM_CAPTURE_HEADERS";
			public const string LogLevel = "ELASTIC_APM_LOG_LEVEL";
			public const string MetricsInterval = "ELASTIC_APM_METRICS_INTERVAL";
			public const string SecretToken = "ELASTIC_APM_SECRET_TOKEN";
			public const string ServerUrls = "ELASTIC_APM_SERVER_URLS";
			public const string ServiceName = "ELASTIC_APM_SERVICE_NAME";
			public const string SpanFramesMinDuration = "ELASTIC_APM_SPAN_FRAMES_MIN_DURATION";
			public const string StackTraceLimit = "ELASTIC_APM_STACK_TRACE_LIMIT";
			public const string TransactionSampleRate = "ELASTIC_APM_TRANSACTION_SAMPLE_RATE";
			public const string CaptureBody = "CAPTURE_BODY";
			public const string CaptureBodyContentTypes = "CAPTURE_BODY_CONTENT_TYPES";
		}

		public static class SupportedValues
		{
			public const string CaptureBodyOff = "off";
			public const string CaptureBodyAll = "all";
			public const string CaptureBodyErrors = "errors";
			public const string CaptureBodyTransactions = "transactions";
			public static List<string> CaptureBodySupportedValues = new List<string>() { CaptureBodyOff, CaptureBodyAll, CaptureBodyErrors, CaptureBodyTransactions };
		}
	}
}
