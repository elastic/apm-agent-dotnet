using System;
using System.Collections.Generic;

namespace Elastic.Apm.Config
{
	public static class ConfigConsts
	{
		public static class EnvVarNames
		{
			public const string LogLevel = "ELASTIC_APM_LOG_LEVEL";
			public const string ServerUrls = "ELASTIC_APM_SERVER_URLS";
			public const string ServiceName = "ELASTIC_APM_SERVICE_NAME";
			public const string SecretToken = "ELASTIC_APM_SECRET_TOKEN";
			public const string CaptureHeaders = "ELASTIC_APM_CAPTURE_HEADERS";
			public const string TransactionSampleRate = "ELASTIC_APM_TRANSACTION_SAMPLE_RATE";
			public const string MetricsInterval = "ELASTIC_APM_METRICS_INTERVAL";
			public const string CaptureBody = "ELASTIC_APM_CAPTURE_BODY";
		}

		public static class DefaultValues
		{
			public const double TransactionSampleRate = 1.0;
			public const string UnknownServiceName = "unknown";
			public const double MetricsIntervalInMilliseconds = 30 * 1000;
			public const string MetricsInterval = "30s";
			public const int ApmServerPort = 8200;
			public static Uri ServerUri => new Uri($"http://localhost:{ApmServerPort}");
			public const string captureBody = SupportedValues.CaptureBodyOff;
		}

		public static class Constraints
		{
			public const double MinMetricsIntervalInMillisecond = 1000;
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
