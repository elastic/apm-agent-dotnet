// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

#nullable enable
using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	/// <summary>
	/// Represents an active configuration snapshot with potential overrides from an updated <see cref="CentralConfiguration"/>
	/// </summary>
	internal class RuntimeConfigurationSnapshot : IConfiguration, IConfigurationDescription
	{
		private readonly CentralConfiguration? _dynamicConfiguration;
		private readonly IConfigurationReader _mainConfiguration;

		internal RuntimeConfigurationSnapshot(IConfigurationReader mainConfiguration, string description)
			: this(mainConfiguration, description, null) {}
		internal RuntimeConfigurationSnapshot(IConfigurationReader mainConfiguration, string description, CentralConfiguration? dynamicConfiguration)
		{
			_mainConfiguration = mainConfiguration;
			_dynamicConfiguration = dynamicConfiguration;
			Description = description;
		}

		public string ApiKey => _mainConfiguration.ApiKey;
		public IReadOnlyCollection<string> ApplicationNamespaces => _mainConfiguration.ApplicationNamespaces;

		public string CaptureBody => _dynamicConfiguration?.CaptureBody ?? _mainConfiguration.CaptureBody;

		public List<string> CaptureBodyContentTypes => _dynamicConfiguration?.CaptureBodyContentTypes ?? _mainConfiguration.CaptureBodyContentTypes;

		public bool CaptureHeaders => _dynamicConfiguration?.CaptureHeaders ?? _mainConfiguration.CaptureHeaders;
		public bool CentralConfig => _mainConfiguration.CentralConfig;

		public string CloudProvider => _mainConfiguration.CloudProvider;

		public string Description { get; }

		public IReadOnlyList<WildcardMatcher> DisableMetrics => _mainConfiguration.DisableMetrics;
		public bool Enabled => _mainConfiguration.Enabled;
		public bool OpenTelemetryBridgeEnabled => _mainConfiguration.OpenTelemetryBridgeEnabled;

		public string Environment => _mainConfiguration.Environment;
		public IReadOnlyCollection<string> ExcludedNamespaces => _mainConfiguration.ExcludedNamespaces;
		public double ExitSpanMinDuration => _dynamicConfiguration?.ExitSpanMinDuration ?? _mainConfiguration.ExitSpanMinDuration;

		public TimeSpan FlushInterval => _mainConfiguration.FlushInterval;

		public IReadOnlyDictionary<string, string> GlobalLabels => _mainConfiguration.GlobalLabels;

		public string HostName => _mainConfiguration.HostName;

		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues => _dynamicConfiguration?.IgnoreMessageQueues ?? _mainConfiguration.IgnoreMessageQueues;

		public LogLevel LogLevel => _dynamicConfiguration?.LogLevel ?? _mainConfiguration.LogLevel;

		public int MaxBatchEventCount => _mainConfiguration.MaxBatchEventCount;

		public int MaxQueueEventCount => _mainConfiguration.MaxQueueEventCount;

		public double MetricsIntervalInMilliseconds => _mainConfiguration.MetricsIntervalInMilliseconds;
		public bool Recording => _dynamicConfiguration?.Recording ?? _mainConfiguration.Recording;

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => _dynamicConfiguration?.SanitizeFieldNames ?? _mainConfiguration.SanitizeFieldNames;

		public string SecretToken => _mainConfiguration.SecretToken;

		public string ServerCert => _mainConfiguration.ServerCert;

		public Uri ServerUrl => _mainConfiguration.ServerUrl;

		[Obsolete("Use ServerUrl")]
		public IReadOnlyList<Uri> ServerUrls => _mainConfiguration.ServerUrls;

		public bool UseWindowsCredentials => _mainConfiguration.UseWindowsCredentials;

		public string ServiceName => _mainConfiguration.ServiceName;
		public string ServiceNodeName => _mainConfiguration.ServiceNodeName;

		public string ServiceVersion => _mainConfiguration.ServiceVersion;
		public bool SpanCompressionEnabled => _dynamicConfiguration?.SpanCompressionEnabled ?? _mainConfiguration.SpanCompressionEnabled;

		public double SpanCompressionExactMatchMaxDuration =>
			_dynamicConfiguration?.SpanCompressionExactMatchMaxDuration ?? _mainConfiguration.SpanCompressionExactMatchMaxDuration;

		public double SpanCompressionSameKindMaxDuration =>
			_dynamicConfiguration?.SpanCompressionSameKindMaxDuration ?? _mainConfiguration.SpanCompressionSameKindMaxDuration;

		public double SpanStackTraceMinDurationInMilliseconds =>
			_dynamicConfiguration?.SpanStackTraceMinDurationInMilliseconds ?? _mainConfiguration.SpanStackTraceMinDurationInMilliseconds;

		[Obsolete("Use SpanStackTraceMinDurationInMilliseconds")]
		public double SpanFramesMinDurationInMilliseconds =>
			_dynamicConfiguration?.SpanFramesMinDurationInMilliseconds ?? _mainConfiguration.SpanFramesMinDurationInMilliseconds;

		public int StackTraceLimit => _dynamicConfiguration?.StackTraceLimit ?? _mainConfiguration.StackTraceLimit;
		[Obsolete("Use TraceContinuationStrategy")]
		public bool TraceContextIgnoreSampledFalse => _mainConfiguration.TraceContextIgnoreSampledFalse;

		public string TraceContinuationStrategy => _dynamicConfiguration?.TraceContinuationStrategy ?? _mainConfiguration.TraceContinuationStrategy;

		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls =>
			_dynamicConfiguration?.TransactionIgnoreUrls ?? _mainConfiguration.TransactionIgnoreUrls;

		public int TransactionMaxSpans => _dynamicConfiguration?.TransactionMaxSpans ?? _mainConfiguration.TransactionMaxSpans;

		public double TransactionSampleRate => _dynamicConfiguration?.TransactionSampleRate ?? _mainConfiguration.TransactionSampleRate;

		public bool UseElasticTraceparentHeader => _mainConfiguration.UseElasticTraceparentHeader;

		public bool VerifyServerCert => _mainConfiguration.VerifyServerCert;
	}
}
