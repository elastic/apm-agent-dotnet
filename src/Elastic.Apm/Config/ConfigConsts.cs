using System;
using System.Collections.Generic;

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
			public const string Prefix = "ELASTIC_APM_";

			public const string CaptureHeaders = Prefix + "CAPTURE_HEADERS";
			public const string LogLevel = Prefix + "LOG_LEVEL";
			public const string MetricsInterval = Prefix + "METRICS_INTERVAL";
			public const string SecretToken = Prefix + "SECRET_TOKEN";
			public const string ServerUrls = Prefix + "SERVER_URLS";
			public const string ServiceName = Prefix + "SERVICE_NAME";
			public const string ServiceVersion = Prefix + "SERVICE_VERSION";
			public const string Environment = Prefix + "ENVIRONMENT";
			public const string SpanFramesMinDuration = Prefix + "SPAN_FRAMES_MIN_DURATION";
			public const string StackTraceLimit = Prefix + "STACK_TRACE_LIMIT";
			public const string TransactionSampleRate = Prefix + "TRANSACTION_SAMPLE_RATE";
			public const string CaptureBody = Prefix + "CAPTURE_BODY";
			public const string CaptureBodyContentTypes = Prefix + "CAPTURE_BODY_CONTENT_TYPES";
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
