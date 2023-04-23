// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	/// <summary> An implementation that looks up configuration keys in an external configuration system </summary>
	internal interface IConfigurationKeyValueProvider : IConfigurationDescription
	{
		/// <summary> Reads a configuration value, may return null to indicate no configuration is provided </summary>
		ApplicationKeyValue Read(ConfigurationOption option);
	}

	/// <summary> An interface that looks up configuration based on environment variables</summary>
	internal interface IConfigurationEnvironmentValueProvider : IConfigurationDescription
	{
		/// <summary>
		/// Returns the environment configuration value for a particular config, implementations should always return an
		/// <see cref="EnvironmentKeyValue"/> instance even if no environment variable is set.
		/// </summary>
		EnvironmentKeyValue Read(ConfigurationOption option);
	}

	internal class EnvironmentKeyValueProvider : IConfigurationEnvironmentValueProvider
	{
		internal const string Origin = "environment variables";

		public string Description => Origin;

		public EnvironmentKeyValue Read(ConfigurationOption option)
		{
			var variable = option.ToEnvironmentVariable();
			var value = Environment.GetEnvironmentVariable(variable)?.Trim();
			return new(option, value, Origin);
		}
	}

	internal class ConfigurationDefaults
	{
		public string ServiceName { get; internal set; }

		public string EnvironmentName { get; internal set; }

		public string DebugName { get; internal set; }
	}


	internal abstract class FallbackToEnvironmentConfigurationBase : FallbackConfigurationBase
	{
		internal FallbackToEnvironmentConfigurationBase(
			IApmLogger logger,
			ConfigurationDefaults defaults,
			IConfigurationKeyValueProvider configKeyValueProvider
		)
			: base(logger, defaults, configKeyValueProvider, new EnvironmentKeyValueProvider()) { }
	}

	internal abstract class FallbackConfigurationBase : AbstractConfigurationReader, IConfigurationReader, IConfigurationLogger
	{
		internal FallbackConfigurationBase(
			IApmLogger logger,
			ConfigurationDefaults defaults,
			IConfigurationKeyValueProvider configKeyValueProvider,
			IConfigurationEnvironmentValueProvider environmentValueProvider,
			string description = null
		)
			: base(logger, defaults)
		{
			KeyValueProvider = configKeyValueProvider;
			EnvironmentProvider = environmentValueProvider;
			Description = description ?? configKeyValueProvider.Description ?? EnvironmentProvider.Description;

			LogLevel = ParseLogLevel(Read(ConfigurationOption.LogLevel));

			Environment = ParseEnvironment(Read(ConfigurationOption.Environment)) ?? defaults?.EnvironmentName;

			ApiKey = ParseApiKey(Read(ConfigurationOption.ApiKey));
			ApplicationNamespaces = ParseApplicationNamespaces(Read(ConfigurationOption.ApplicationNamespaces));
			CaptureBody = ParseCaptureBody(Read(ConfigurationOption.CaptureBody));
			CaptureBodyContentTypes = ParseCaptureBodyContentTypes(Read(ConfigurationOption.CaptureBodyContentTypes));
			CaptureHeaders = ParseCaptureHeaders(Read(ConfigurationOption.CaptureHeaders));
			CentralConfig = ParseCentralConfig(Read(ConfigurationOption.CentralConfig));
			CloudProvider = ParseCloudProvider(Read(ConfigurationOption.CloudProvider));
			DisableMetrics = ParseDisableMetrics(Read(ConfigurationOption.DisableMetrics));
			Enabled = ParseEnabled(Read(ConfigurationOption.Enabled));
			OpenTelemetryBridgeEnabled =
				ParseOpenTelemetryBridgeEnabled(Read(ConfigurationOption.OpenTelemetryBridgeEnabled));
			ExcludedNamespaces = ParseExcludedNamespaces(Read(ConfigurationOption.ExcludedNamespaces));
			ExitSpanMinDuration = ParseExitSpanMinDuration(Read(ConfigurationOption.ExitSpanMinDuration));
			FlushInterval = ParseFlushInterval(Read(ConfigurationOption.FlushInterval));
			GlobalLabels = ParseGlobalLabels(Read(ConfigurationOption.GlobalLabels));
			HostName = ParseHostName(Read(ConfigurationOption.HostName));
			IgnoreMessageQueues = ParseIgnoreMessageQueues(Read(ConfigurationOption.IgnoreMessageQueues));
			MaxBatchEventCount = ParseMaxBatchEventCount(Read(ConfigurationOption.MaxBatchEventCount));
			MaxQueueEventCount = ParseMaxQueueEventCount(Read(ConfigurationOption.MaxQueueEventCount));
			MetricsIntervalInMilliseconds = ParseMetricsInterval(Read(ConfigurationOption.MetricsInterval));
			Recording = ParseRecording(Read(ConfigurationOption.Recording));
			SanitizeFieldNames = ParseSanitizeFieldNames(Read(ConfigurationOption.SanitizeFieldNames));
			SecretToken = ParseSecretToken(Read(ConfigurationOption.SecretToken));
			ServerCert = ParseServerCert(Read(ConfigurationOption.ServerCert));
			UseWindowsCredentials = ParseUseWindowsCredentials(Read(ConfigurationOption.UseWindowsCredentials));
			ServiceName = ParseServiceName(Read(ConfigurationOption.ServiceName));
			ServiceNodeName = ParseServiceNodeName(Read(ConfigurationOption.ServiceNodeName));
			ServiceVersion = ParseServiceVersion(Read(ConfigurationOption.ServiceVersion));
			SpanCompressionEnabled = ParseSpanCompressionEnabled(Read(ConfigurationOption.SpanCompressionEnabled));
			SpanCompressionExactMatchMaxDuration =
				ParseSpanCompressionExactMatchMaxDuration(Read(ConfigurationOption.SpanCompressionExactMatchMaxDuration));
			SpanCompressionSameKindMaxDuration =
				ParseSpanCompressionSameKindMaxDuration(Read(ConfigurationOption.SpanCompressionSameKindMaxDuration));
#pragma warning disable CS0618
			SpanFramesMinDurationInMilliseconds =
				ParseSpanFramesMinDurationInMilliseconds(Read(ConfigurationOption.SpanFramesMinDuration));
#pragma warning restore CS0618
			SpanStackTraceMinDurationInMilliseconds =
				ParseSpanStackTraceMinDurationInMilliseconds(Read(ConfigurationOption.SpanStackTraceMinDuration));
			StackTraceLimit = ParseStackTraceLimit(Read(ConfigurationOption.StackTraceLimit));
			TraceContextIgnoreSampledFalse =
				ParseTraceContextIgnoreSampledFalse(Read(ConfigurationOption.TraceContextIgnoreSampledFalse));
			TraceContinuationStrategy =
				ParseTraceContinuationStrategy(Read(ConfigurationOption.TraceContinuationStrategy));
			TransactionIgnoreUrls =
				ParseTransactionIgnoreUrls(Read(ConfigurationOption.TransactionIgnoreUrls));
			TransactionMaxSpans = ParseTransactionMaxSpans(Read(ConfigurationOption.TransactionMaxSpans));
			TransactionSampleRate = ParseTransactionSampleRate(Read(ConfigurationOption.TransactionSampleRate));
			UseElasticTraceparentHeader =
				ParseUseElasticTraceparentHeader(Read(ConfigurationOption.UseElasticTraceparentHeader));
			VerifyServerCert = ParseVerifyServerCert(Read(ConfigurationOption.VerifyServerCert));

			var urlConfig = Read(ConfigurationOption.ServerUrl);
			var urlsConfig = Read(ConfigurationOption.ServerUrls);
#pragma warning disable CS0618
			ServerUrls = ParseServerUrls(!string.IsNullOrEmpty(urlsConfig.Value) ? urlsConfig : urlConfig);
			ServerUrl = !string.IsNullOrEmpty(urlConfig.Value) ? ParseServerUrl(urlConfig) : ServerUrls.FirstOrDefault();
#pragma warning restore CS0618
		}

		private IConfigurationKeyValueProvider KeyValueProvider { get; }

		protected IConfigurationEnvironmentValueProvider EnvironmentProvider { get; }

		public ConfigurationKeyValue GetConfiguration(ConfigurationOption option) =>
			KeyValueProvider.Read(option) as ConfigurationKeyValue ?? EnvironmentProvider.Read(option);

		protected ConfigurationKeyValue Read(ConfigurationOption option) =>
			KeyValueProvider.Read(option) as ConfigurationKeyValue ?? EnvironmentProvider.Read(option);

		public string Description { get; }

		public string ApiKey { get; }

		public IReadOnlyCollection<string> ApplicationNamespaces { get; }

		public string CaptureBody { get; }

		public List<string> CaptureBodyContentTypes { get; }

		public bool CaptureHeaders { get; }

		public bool CentralConfig { get; }

		public string CloudProvider { get; }

		public IReadOnlyList<WildcardMatcher> DisableMetrics { get; }

		public bool Enabled { get; }

		public string Environment { get; }

		public IReadOnlyCollection<string> ExcludedNamespaces { get; }

		public double ExitSpanMinDuration { get; }

		public TimeSpan FlushInterval { get; }

		public IReadOnlyDictionary<string, string> GlobalLabels { get; }

		public string HostName { get; }

		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues { get; }

		public LogLevel LogLevel { get; protected set; }

		public int MaxBatchEventCount { get; }

		public int MaxQueueEventCount { get; }

		public double MetricsIntervalInMilliseconds { get; }

		public bool Recording { get; }

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; }

		public string SecretToken { get; }

		public string ServerCert { get; }

		/// <inheritdoc />
		public Uri ServerUrl { get; }

		/// <inheritdoc />
		[Obsolete("Use ServerUrl")]
		public IReadOnlyList<Uri> ServerUrls { get; }

		public bool UseWindowsCredentials { get; }

		public string ServiceName { get; protected set; }

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

		public bool OpenTelemetryBridgeEnabled { get; }
	}
}
