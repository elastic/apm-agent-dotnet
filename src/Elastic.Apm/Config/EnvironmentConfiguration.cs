// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Config
{
	internal class EnvironmentKeyValueProvider : IConfigurationKeyValueProvider
	{
		internal const string Origin = "environment variables";

		public ConfigurationKeyValue Read(string key) => new(key, ReadEnvVarValue(key), Origin);

		private static string ReadEnvVarValue(string variable) => Environment.GetEnvironmentVariable(variable)?.Trim();
	}

	internal class EnvironmentConfiguration
		: AbstractConfigurationReader, IConfiguration, IConfigurationSnapshotDescription, IConfigurationLoggingPreambleProvider
	{
		private const string ThisClassName = nameof(EnvironmentConfiguration);

		public EnvironmentConfiguration(IApmLogger logger = null) : base(logger, ThisClassName)
		{
			Description = EnvironmentKeyValueProvider.Origin;

			KeyValueProvider = new EnvironmentKeyValueProvider();
			ConfigurationKeyValue Read(string variable) => KeyValueProvider.Read(variable);

			LogLevel = ParseLogLevel(Read(EnvVarNames.LogLevel));

			ApiKey = ParseApiKey(Read(EnvVarNames.ApiKey));
			ApplicationNamespaces = ParseApplicationNamespaces(Read(EnvVarNames.ApplicationNamespaces));
			CaptureBody = ParseCaptureBody(Read(EnvVarNames.CaptureBody));
			CaptureBodyContentTypes = ParseCaptureBodyContentTypes(Read(EnvVarNames.CaptureBodyContentTypes));
			CaptureHeaders = ParseCaptureHeaders(Read(EnvVarNames.CaptureHeaders));
			CentralConfig = ParseCentralConfig(Read(EnvVarNames.CentralConfig));
			CloudProvider = ParseCloudProvider(Read(EnvVarNames.CloudProvider));
			DisableMetrics = ParseDisableMetrics(Read(EnvVarNames.DisableMetrics));
			Enabled = ParseEnabled(Read(EnvVarNames.Enabled));
			OpenTelemetryBridgeEnabled = ParseOpenTelemetryBridgeEnabled(Read(EnvVarNames.OpenTelemetryBridgeEnabled));
			Environment = ParseEnvironment(Read(EnvVarNames.Environment));
			ExcludedNamespaces = ParseExcludedNamespaces(Read(EnvVarNames.ExcludedNamespaces));
			ExitSpanMinDuration = ParseExitSpanMinDuration(Read(EnvVarNames.ExitSpanMinDuration));
			FlushInterval = ParseFlushInterval(Read(EnvVarNames.FlushInterval));
			GlobalLabels = ParseGlobalLabels(Read(EnvVarNames.GlobalLabels));
			HostName = ParseHostName(Read(EnvVarNames.HostName));
			IgnoreMessageQueues = ParseIgnoreMessageQueues(Read(EnvVarNames.IgnoreMessageQueues));
			MaxBatchEventCount = ParseMaxBatchEventCount(Read(EnvVarNames.MaxBatchEventCount));
			MaxQueueEventCount = ParseMaxQueueEventCount(Read(EnvVarNames.MaxQueueEventCount));
			MetricsIntervalInMilliseconds = ParseMetricsInterval(Read(EnvVarNames.MetricsInterval));
			Recording = ParseRecording(Read(EnvVarNames.Recording));
			SanitizeFieldNames = ParseSanitizeFieldNames(Read(EnvVarNames.SanitizeFieldNames));
			SecretToken = ParseSecretToken(Read(EnvVarNames.SecretToken));
			ServerCert = ParseServerCert(Read(EnvVarNames.ServerCert));
			UseWindowsCredentials = ParseUseWindowsCredentials(Read(EnvVarNames.UseWindowsCredentials));
			ServiceName = ParseServiceName(Read(EnvVarNames.ServiceName));
			ServiceNodeName = ParseServiceNodeName(Read(EnvVarNames.ServiceNodeName));
			ServiceVersion = ParseServiceVersion(Read(EnvVarNames.ServiceVersion));
			SpanCompressionEnabled = ParseSpanCompressionEnabled(Read(EnvVarNames.SpanCompressionEnabled));
			SpanCompressionExactMatchMaxDuration =
				ParseSpanCompressionExactMatchMaxDuration(Read(EnvVarNames.SpanCompressionExactMatchMaxDuration));
			SpanCompressionSameKindMaxDuration =
				ParseSpanCompressionSameKindMaxDuration(Read(EnvVarNames.SpanCompressionSameKindMaxDuration));
#pragma warning disable CS0618
			SpanFramesMinDurationInMilliseconds = ParseSpanFramesMinDurationInMilliseconds(Read(EnvVarNames.SpanFramesMinDuration));
#pragma warning restore CS0618
			SpanStackTraceMinDurationInMilliseconds =
				ParseSpanStackTraceMinDurationInMilliseconds(Read(EnvVarNames.SpanStackTraceMinDuration));
			StackTraceLimit = ParseStackTraceLimit(Read(EnvVarNames.StackTraceLimit));
			TraceContextIgnoreSampledFalse =
				ParseTraceContextIgnoreSampledFalse(Read(EnvVarNames.TraceContextIgnoreSampledFalse));
			TraceContinuationStrategy = ParseTraceContinuationStrategy(Read(EnvVarNames.TraceContinuationStrategy));
			TransactionIgnoreUrls =
				ParseTransactionIgnoreUrls(Read(EnvVarNames.TransactionIgnoreUrls));
			TransactionMaxSpans = ParseTransactionMaxSpans(Read(EnvVarNames.TransactionMaxSpans));
			TransactionSampleRate = ParseTransactionSampleRate(Read(EnvVarNames.TransactionSampleRate));
			UseElasticTraceparentHeader = ParseUseElasticTraceparentHeader(Read(EnvVarNames.UseElasticTraceparentHeader));
			VerifyServerCert = ParseVerifyServerCert(Read(EnvVarNames.VerifyServerCert));

			var urlConfig = Read(EnvVarNames.ServerUrl);
			var urlsConfig = Read(EnvVarNames.ServerUrls);
#pragma warning disable CS0618
			ServerUrls = ParseServerUrls(!string.IsNullOrEmpty(urlsConfig.Value) ? urlsConfig : urlConfig);
			ServerUrl = !string.IsNullOrEmpty(urlConfig.Value) ? ParseServerUrl(urlConfig) : ServerUrls.FirstOrDefault();
#pragma warning restore CS0618
		}

		private EnvironmentKeyValueProvider KeyValueProvider { get; }

		public ConfigurationKeyValue Get(ConfigurationItem item) => KeyValueProvider.Read(item.EnvironmentVariableName);

		public string ApiKey { get; }
		public IReadOnlyCollection<string> ApplicationNamespaces { get; }
		public string CaptureBody { get; }
		public List<string> CaptureBodyContentTypes { get; }
		public bool CaptureHeaders { get; }
		public bool CentralConfig { get; }
		public string CloudProvider { get; }
		public IReadOnlyList<WildcardMatcher> DisableMetrics { get; }
		public bool Enabled { get; }
		public bool OpenTelemetryBridgeEnabled { get; }
		public string Environment { get; }
		public IReadOnlyCollection<string> ExcludedNamespaces { get; }
		public double ExitSpanMinDuration { get; }
		public TimeSpan FlushInterval { get; }
		public IReadOnlyDictionary<string, string> GlobalLabels { get; }
		public string HostName { get; }
		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues { get; }
		public LogLevel LogLevel { get; }
		public int MaxBatchEventCount { get; }
		public int MaxQueueEventCount { get; }
		public double MetricsIntervalInMilliseconds { get; }
		public bool Recording { get; }
		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; }
		public string SecretToken { get; }
		public string ServerCert { get; }
		public Uri ServerUrl { get; }

		/// <inheritdoc />
		[Obsolete("Use ServerUrl")]
		public IReadOnlyList<Uri> ServerUrls { get; }

		public bool UseWindowsCredentials { get; }
		public string ServiceName { get; }
		public string ServiceNodeName { get; }
		public string ServiceVersion { get; }
		public bool SpanCompressionEnabled { get; }
		public double SpanCompressionExactMatchMaxDuration { get; }
		public double SpanCompressionSameKindMaxDuration { get; }
		public double SpanStackTraceMinDurationInMilliseconds { get; }

		[Obsolete("Use SpanStackTraceMinDurationInMilliseconds")]
		public double SpanFramesMinDurationInMilliseconds { get; }

		public int StackTraceLimit { get; }
		public bool TraceContextIgnoreSampledFalse { get; }
		public string TraceContinuationStrategy { get; }
		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls { get; }
		public int TransactionMaxSpans { get; }
		public double TransactionSampleRate { get; }
		public bool UseElasticTraceparentHeader { get; }
		public bool VerifyServerCert { get; }

		public string Description { get; }
	}
}
