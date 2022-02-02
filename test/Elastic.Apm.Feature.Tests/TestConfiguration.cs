// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Feature.Tests
{
	public class TestConfiguration : IConfiguration
	{
		public string ApiKey { get; set; }
		public IReadOnlyCollection<string> ApplicationNamespaces { get; set; } = DefaultValues.DefaultApplicationNamespaces;
		public string CaptureBody { get; set; } = SupportedValues.CaptureBodyOff;
		public List<string> CaptureBodyContentTypes { get; set; } = new()
		{
			"application/x-www-form-urlencoded*",
			"text/*",
			"application/json*, application/xml*"
		};

		public bool CaptureHeaders { get; set; } = DefaultValues.CaptureHeaders;
		public bool CentralConfig { get; set; } = DefaultValues.CentralConfig;
		public string CloudProvider { get; set; } = SupportedValues.CloudProviderNone;
		public IReadOnlyList<WildcardMatcher> DisableMetrics { get; set; } = new List<WildcardMatcher>();
		public bool Enabled { get; set; } = true;
		public string Environment { get; set; }
		public IReadOnlyCollection<string> ExcludedNamespaces { get; set; } = DefaultValues.DefaultExcludedNamespaces;
		public TimeSpan FlushInterval { get; set; } = TimeSpan.Zero;
		public IReadOnlyDictionary<string, string> GlobalLabels { get; set; } = new Dictionary<string, string>();
		public string HostName { get; set; }
		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues { get; set; } = DefaultValues.IgnoreMessageQueues;
		public LogLevel LogLevel { get; set; } = DefaultValues.LogLevel;
		public int MaxBatchEventCount { get; set; } = DefaultValues.MaxBatchEventCount;
		public int MaxQueueEventCount { get; set; } = DefaultValues.MaxQueueEventCount;
		public double MetricsIntervalInMilliseconds { get; set; } = DefaultValues.MetricsIntervalInMilliseconds;
		public bool Recording { get; set; } = true;
		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; set; } = DefaultValues.SanitizeFieldNames;
		public string SecretToken { get; set; }
		public string ServerCert { get; set; }
		public Uri ServerUrl { get; set; } = DefaultValues.ServerUri;
		public IReadOnlyList<Uri> ServerUrls { get; set; } = new List<Uri> { DefaultValues.ServerUri };
		public string ServiceName { get; set; }
		public string ServiceNodeName { get; set; }
		public string ServiceVersion { get; set; }
		public bool SpanCompressionEnabled => DefaultValues.SpanCompressionEnabled;
		public double SpanCompressionExactMatchMaxDuration => DefaultValues.SpanCompressionExactMatchMaxDurationInMilliseconds;
		public double SpanCompressionSameKindMaxDuration => DefaultValues.SpanCompressionSameKindMaxDurationInMilliseconds;
		public double SpanFramesMinDurationInMilliseconds { get; set; } = DefaultValues.SpanFramesMinDurationInMilliseconds;
		public int StackTraceLimit { get; set; } = DefaultValues.StackTraceLimit;
		public bool TraceContextIgnoreSampledFalse { get; set; } = DefaultValues.TraceContextIgnoreSampledFalse;
		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls { get; set; } = DefaultValues.TransactionIgnoreUrls;
		public int TransactionMaxSpans { get; set; } = DefaultValues.TransactionMaxSpans;
		public double TransactionSampleRate { get; set; } = DefaultValues.TransactionSampleRate;
		public bool UseElasticTraceparentHeader { get; set; } = DefaultValues.UseElasticTraceparentHeader;
		public bool VerifyServerCert { get; set; } = DefaultValues.VerifyServerCert;
		public bool EnableOpenTelemetryBridge { get; set; }
	}
}
