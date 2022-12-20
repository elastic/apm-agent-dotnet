// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class EnvironmentConfigurationReader : AbstractConfigurationReader, IConfiguration,
		IConfigurationSnapshotDescription, IConfigurationMetaDataProvider
	{
		internal const string Origin = "environment variables";
		private const string ThisClassName = nameof(EnvironmentConfigurationReader);
		private readonly Lazy<double> _spanFramesMinDurationInMilliseconds;
		private readonly Lazy<double> _spanStackTraceMinDurationInMilliseconds;
		private readonly Lazy<int> _stackTraceLimit;

		public EnvironmentConfigurationReader(IApmLogger logger = null) : base(logger, ThisClassName)
		{
			_spanFramesMinDurationInMilliseconds
				= new Lazy<double>(() =>
					ParseSpanFramesMinDurationInMilliseconds(Read(ConfigConsts.EnvVarNames.SpanFramesMinDuration)));

			_spanStackTraceMinDurationInMilliseconds
				= new Lazy<double>(() =>
					ParseSpanStackTraceMinDurationInMilliseconds(Read(ConfigConsts.EnvVarNames.SpanStackTraceMinDuration)));

			_stackTraceLimit = new Lazy<int>(() => ParseStackTraceLimit(Read(ConfigConsts.EnvVarNames.StackTraceLimit)));
		}

		public string ApiKey => ParseApiKey(Read(ConfigConsts.EnvVarNames.ApiKey));

		public IReadOnlyCollection<string> ApplicationNamespaces => ParseApplicationNamespaces(Read(ConfigConsts.EnvVarNames.ApplicationNamespaces));

		public string CaptureBody => ParseCaptureBody(Read(ConfigConsts.EnvVarNames.CaptureBody));

		public List<string> CaptureBodyContentTypes => ParseCaptureBodyContentTypes(Read(ConfigConsts.EnvVarNames.CaptureBodyContentTypes));

		public bool CaptureHeaders => ParseCaptureHeaders(Read(ConfigConsts.EnvVarNames.CaptureHeaders));

		public bool CentralConfig => ParseCentralConfig(Read(ConfigConsts.EnvVarNames.CentralConfig));

		public string CloudProvider => ParseCloudProvider(Read(ConfigConsts.EnvVarNames.CloudProvider));

		public string Description => Origin;
		public IReadOnlyList<WildcardMatcher> DisableMetrics => ParseDisableMetrics(Read(ConfigConsts.EnvVarNames.DisableMetrics));
		public bool Enabled => ParseEnabled(Read(ConfigConsts.EnvVarNames.Enabled));
		public bool EnableOpenTelemetryBridge => ParseEnableOpenTelemetryBridge(Read(ConfigConsts.EnvVarNames.EnableOpenTelemetryBridge));

		public string Environment => ParseEnvironment(Read(ConfigConsts.EnvVarNames.Environment));

		public IReadOnlyCollection<string> ExcludedNamespaces => ParseExcludedNamespaces(Read(ConfigConsts.EnvVarNames.ExcludedNamespaces));
		public double ExitSpanMinDuration => ParseExitSpanMinDuration(Read(ConfigConsts.EnvVarNames.ExitSpanMinDuration));

		public TimeSpan FlushInterval => ParseFlushInterval(Read(ConfigConsts.EnvVarNames.FlushInterval));

		public IReadOnlyDictionary<string, string> GlobalLabels => ParseGlobalLabels(Read(ConfigConsts.EnvVarNames.GlobalLabels));

		public string HostName => ParseHostName(Read(ConfigConsts.EnvVarNames.HostName));

		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues => ParseIgnoreMessageQueues(Read(ConfigConsts.EnvVarNames.IgnoreMessageQueues));

		public LogLevel LogLevel => ParseLogLevel(Read(ConfigConsts.EnvVarNames.LogLevel));

		public int MaxBatchEventCount => ParseMaxBatchEventCount(Read(ConfigConsts.EnvVarNames.MaxBatchEventCount));

		public int MaxQueueEventCount => ParseMaxQueueEventCount(Read(ConfigConsts.EnvVarNames.MaxQueueEventCount));

		public double MetricsIntervalInMilliseconds => ParseMetricsInterval(Read(ConfigConsts.EnvVarNames.MetricsInterval));
		public bool Recording => ParseRecording(Read(ConfigConsts.EnvVarNames.Recording));

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => ParseSanitizeFieldNames(Read(ConfigConsts.EnvVarNames.SanitizeFieldNames));

		public string SecretToken => ParseSecretToken(Read(ConfigConsts.EnvVarNames.SecretToken));

		public string ServerCert => ParseServerCert(Read(ConfigConsts.EnvVarNames.ServerCert));

		/// <inheritdoc />
		public Uri ServerUrl
		{
			get
			{
				// Fallback to using the first ServerUrls in the event ServerUrl is not specified
				var configurationKeyValue = Read(ConfigConsts.EnvVarNames.ServerUrl);
				return !string.IsNullOrEmpty(configurationKeyValue.Value)
					? ParseServerUrl(configurationKeyValue)
#pragma warning disable 618
					: ServerUrls[0];
#pragma warning restore 618
			}
		}

		/// <inheritdoc />
		[Obsolete("Use ServerUrl")]
		public IReadOnlyList<Uri> ServerUrls
		{
			get
			{
				// Use ServerUrl if there's no value for ServerUrls so that usage of ServerUrls
				// outside of the agent will work with ServerUrl
				var configurationKeyValue = Read(ConfigConsts.EnvVarNames.ServerUrls);
				return ParseServerUrls(!string.IsNullOrEmpty(configurationKeyValue.Value)
					? configurationKeyValue
					: Read(ConfigConsts.EnvVarNames.ServerUrl));
			}
		}

		public bool UseWindowsCredentials => ParseUseWindowsCredentials(Read(ConfigConsts.EnvVarNames.UseWindowsCredentials));

		public string ServiceName => ParseServiceName(Read(ConfigConsts.EnvVarNames.ServiceName));

		public string ServiceNodeName => ParseServiceNodeName(Read(ConfigConsts.EnvVarNames.ServiceNodeName));

		public string ServiceVersion => ParseServiceVersion(Read(ConfigConsts.EnvVarNames.ServiceVersion));
		public bool SpanCompressionEnabled => ParseSpanCompressionEnabled(Read(ConfigConsts.EnvVarNames.SpanCompressionEnabled));

		public double SpanCompressionExactMatchMaxDuration =>
			ParseSpanCompressionExactMatchMaxDuration(Read(ConfigConsts.EnvVarNames.SpanCompressionExactMatchMaxDuration));

		public double SpanCompressionSameKindMaxDuration =>
			ParseSpanCompressionSameKindMaxDuration(Read(ConfigConsts.EnvVarNames.SpanCompressionSameKindMaxDuration));

		public double SpanStackTraceMinDurationInMilliseconds => _spanStackTraceMinDurationInMilliseconds.Value;

		[Obsolete("Use SpanStackTraceMinDurationInMilliseconds")]
		public double SpanFramesMinDurationInMilliseconds => _spanFramesMinDurationInMilliseconds.Value;

		public int StackTraceLimit => _stackTraceLimit.Value;

		public bool TraceContextIgnoreSampledFalse =>
			ParseTraceContextIgnoreSampledFalse(Read(ConfigConsts.EnvVarNames.TraceContextIgnoreSampledFalse));

		public string TraceContinuationStrategy => ParseTraceContinuationStrategy(Read(ConfigConsts.EnvVarNames.TraceContinuationStrategy));

		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls =>
			ParseTransactionIgnoreUrls(Read(ConfigConsts.EnvVarNames.TransactionIgnoreUrls));

		public int TransactionMaxSpans => ParseTransactionMaxSpans(Read(ConfigConsts.EnvVarNames.TransactionMaxSpans));

		public double TransactionSampleRate => ParseTransactionSampleRate(Read(ConfigConsts.EnvVarNames.TransactionSampleRate));

		public bool UseElasticTraceparentHeader => ParseUseElasticTraceparentHeader(Read(ConfigConsts.EnvVarNames.UseElasticTraceparentHeader));

		public bool VerifyServerCert => ParseVerifyServerCert(Read(ConfigConsts.EnvVarNames.VerifyServerCert));

		private ConfigurationKeyValue Read(string key) => new(key, ReadEnvVarValue(key), Origin);

		public ConfigurationKeyValue Get(ConfigurationItem item) => Read(item.EnvironmentVariableName);
	}
}
