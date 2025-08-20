// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigurationOption;
using Environment = System.Environment;

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
		public string Description => nameof(EnvironmentKeyValueProvider);

		public EnvironmentKeyValue Read(ConfigurationOption option)
		{
			var variable = option.ToEnvironmentVariable();
			var value = Environment.GetEnvironmentVariable(variable)?.Trim();
			return new(option, value, Description);
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

	internal abstract class FallbackConfigurationBase : AbstractConfigurationReader, IConfigurationReader
	{
		internal FallbackConfigurationBase(
			IApmLogger logger,
			ConfigurationDefaults defaults,
			IConfigurationKeyValueProvider configKeyValueProvider,
			IConfigurationEnvironmentValueProvider environmentValueProvider
		)
			: base(logger, defaults)
		{
			KeyValueProvider = configKeyValueProvider;
			EnvironmentProvider = environmentValueProvider;
			var debugName = defaults?.DebugName ?? GetType().Name;
			var kvDebugName = configKeyValueProvider.Description ?? configKeyValueProvider.GetType().Name;
			var envDebugName = environmentValueProvider.Description ?? environmentValueProvider.GetType().Name;
			Description = $"{debugName} (config provider: {kvDebugName} environment provider: {envDebugName})";

			LogLevel = ParseLogLevel(Lookup(ConfigurationOption.LogLevel));

			Environment = ParseEnvironment(Lookup(ConfigurationOption.Environment)) ?? defaults?.EnvironmentName;

			ApiKey = ParseApiKey(Lookup(ConfigurationOption.ApiKey));
			ApplicationNamespaces = ParseApplicationNamespaces(Lookup(ConfigurationOption.ApplicationNamespaces));
			BaggageToAttach = ParseBaggageToAttach(Lookup(ConfigurationOption.BaggageToAttach));
			CaptureBody = ParseCaptureBody(Lookup(ConfigurationOption.CaptureBody));
			CaptureBodyContentTypes = ParseCaptureBodyContentTypes(Lookup(ConfigurationOption.CaptureBodyContentTypes));
			CaptureHeaders = ParseCaptureHeaders(Lookup(ConfigurationOption.CaptureHeaders));
			CentralConfig = ParseCentralConfig(Lookup(ConfigurationOption.CentralConfig));
			CloudProvider = ParseCloudProvider(Lookup(ConfigurationOption.CloudProvider));
			DisableMetrics = ParseDisableMetrics(Lookup(ConfigurationOption.DisableMetrics));
			Enabled = ParseEnabled(Lookup(ConfigurationOption.Enabled));
			OpenTelemetryBridgeEnabled =
				ParseOpenTelemetryBridgeEnabled(Lookup(ConfigurationOption.OpenTelemetryBridgeEnabled));
			ExcludedNamespaces = ParseExcludedNamespaces(Lookup(ConfigurationOption.ExcludedNamespaces));
			ExitSpanMinDuration = ParseExitSpanMinDuration(Lookup(ConfigurationOption.ExitSpanMinDuration));
			FlushInterval = ParseFlushInterval(Lookup(ConfigurationOption.FlushInterval));
			GlobalLabels = ParseGlobalLabels(Lookup(ConfigurationOption.GlobalLabels));
			HostName = ParseHostName(Lookup(ConfigurationOption.HostName));
			IgnoreMessageQueues = ParseIgnoreMessageQueues(Lookup(ConfigurationOption.IgnoreMessageQueues));
			MaxBatchEventCount = ParseMaxBatchEventCount(Lookup(ConfigurationOption.MaxBatchEventCount));
			MaxQueueEventCount = ParseMaxQueueEventCount(Lookup(ConfigurationOption.MaxQueueEventCount));
			MetricsIntervalInMilliseconds = ParseMetricsInterval(Lookup(MetricsInterval));
			Recording = ParseRecording(Lookup(ConfigurationOption.Recording));
			SanitizeFieldNames = ParseSanitizeFieldNames(Lookup(ConfigurationOption.SanitizeFieldNames));
			SecretToken = ParseSecretToken(Lookup(ConfigurationOption.SecretToken));
			ServerCert = ParseServerCert(Lookup(ConfigurationOption.ServerCert));
			UseWindowsCredentials = ParseUseWindowsCredentials(Lookup(ConfigurationOption.UseWindowsCredentials));
			ServiceName = ParseServiceName(Lookup(ConfigurationOption.ServiceName));
			ServiceNodeName = ParseServiceNodeName(Lookup(ConfigurationOption.ServiceNodeName));
			ServiceVersion = ParseServiceVersion(Lookup(ConfigurationOption.ServiceVersion));
			SpanCompressionEnabled = ParseSpanCompressionEnabled(Lookup(ConfigurationOption.SpanCompressionEnabled));
			SpanCompressionExactMatchMaxDuration =
				ParseSpanCompressionExactMatchMaxDuration(Lookup(ConfigurationOption.SpanCompressionExactMatchMaxDuration));
			SpanCompressionSameKindMaxDuration =
				ParseSpanCompressionSameKindMaxDuration(Lookup(ConfigurationOption.SpanCompressionSameKindMaxDuration));
#pragma warning disable CS0618
			SpanFramesMinDurationInMilliseconds =
				ParseSpanFramesMinDurationInMilliseconds(Lookup(SpanFramesMinDuration));
#pragma warning restore CS0618
			SpanStackTraceMinDurationInMilliseconds =
				ParseSpanStackTraceMinDurationInMilliseconds(Lookup(SpanStackTraceMinDuration));
			StackTraceLimit = ParseStackTraceLimit(Lookup(ConfigurationOption.StackTraceLimit));
			TraceContextIgnoreSampledFalse =
				ParseTraceContextIgnoreSampledFalse(Lookup(ConfigurationOption.TraceContextIgnoreSampledFalse));
			TraceContinuationStrategy =
				ParseTraceContinuationStrategy(Lookup(ConfigurationOption.TraceContinuationStrategy));
			TransactionIgnoreUrls =
				ParseTransactionIgnoreUrls(Lookup(ConfigurationOption.TransactionIgnoreUrls));
			TransactionNameGroups =
				ParseTransactionNameGroups(Lookup(ConfigurationOption.TransactionNameGroups));
			TransactionMaxSpans = ParseTransactionMaxSpans(Lookup(ConfigurationOption.TransactionMaxSpans));
			TransactionSampleRate = ParseTransactionSampleRate(Lookup(ConfigurationOption.TransactionSampleRate));
			UseElasticTraceparentHeader =
				ParseUseElasticTraceparentHeader(Lookup(ConfigurationOption.UseElasticTraceparentHeader));
			UsePathAsTransactionName =
				ParseUsePathAsTransactionName(Lookup(ConfigurationOption.UsePathAsTransactionName));
			VerifyServerCert = ParseVerifyServerCert(Lookup(ConfigurationOption.VerifyServerCert));

			var urlConfig = Lookup(ConfigurationOption.ServerUrl);
			var urlsConfig = Lookup(ConfigurationOption.ServerUrls);
#pragma warning disable CS0618
			ServerUrls = ParseServerUrls(!string.IsNullOrEmpty(urlsConfig.Value) ? urlsConfig : urlConfig);
			ServerUrl = !string.IsNullOrEmpty(urlConfig.Value) ? ParseServerUrl(urlConfig) : ServerUrls.FirstOrDefault();
#pragma warning restore CS0618

			ProxyUrl = ParseProxyServerUrl(Lookup(ConfigurationOption.ProxyUrl));
			ProxyUserName = Lookup(ConfigurationOption.ProxyUserName)?.Value;
			ProxyPassword = Lookup(ConfigurationOption.ProxyPassword)?.Value;
		}

		private IConfigurationKeyValueProvider KeyValueProvider { get; }

		private IConfigurationEnvironmentValueProvider EnvironmentProvider { get; }

		public ConfigurationKeyValue Lookup(ConfigurationOption option) =>
			KeyValueProvider.Read(option) as ConfigurationKeyValue ?? EnvironmentProvider.Read(option);

		public string Description { get; }

		public string ApiKey { get; }

		public IReadOnlyCollection<string> ApplicationNamespaces { get; }

		public IReadOnlyList<WildcardMatcher> BaggageToAttach { get; }

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

		public IReadOnlyCollection<WildcardMatcher> TransactionNameGroups { get; }

		public int TransactionMaxSpans { get; }

		public double TransactionSampleRate { get; }

		public bool UseElasticTraceparentHeader { get; }

		public bool UsePathAsTransactionName { get; }

		public bool VerifyServerCert { get; }

		public bool OpenTelemetryBridgeEnabled { get; }

		public Uri ProxyUrl { get; }

		public string ProxyUserName { get; }

		public string ProxyPassword { get; }
	}
}
