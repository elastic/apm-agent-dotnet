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
			public const string BaggageToAttach = "*";
			public const string CaptureBody = SupportedValues.CaptureBodyOff;
			public const string CaptureBodyContentTypes = "application/x-www-form-urlencoded*, text/*, application/json*, application/xml*";
			public const bool CaptureHeaders = true;
			public const bool CentralConfig = true;
			public const string CloudProvider = SupportedValues.CloudProviderAuto;
			public const bool OpenTelemetryBridgeEnabled = true;
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
