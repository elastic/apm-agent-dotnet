// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using static Elastic.Apm.Config.ConfigConsts;
using static Elastic.Apm.Config.ConfigurationItemId;

namespace Elastic.Apm.Config
{
	internal enum ConfigurationItemId
	{
		ApiKey,
		ApplicationNamespaces,
		CaptureBody,
		CaptureBodyContentTypes,
		CaptureHeaders,
		CentralConfig,
		CloudProvider,
		DisableMetrics,
		Enabled,
		OpenTelemetryBridgeEnabled,
		Environment,
		ExcludedNamespaces,
		ExitSpanMinDuration,
		FlushInterval,
		GlobalLabels,
		HostName,
		IgnoreMessageQueues,
		LogLevel,
		MaxBatchEventCount,
		MaxQueueEventCount,
		MetricsInterval,
		Recording,
		SanitizeFieldNames,
		SecretToken,
		ServerCert,
		ServerUrl,
		UseWindowsCredentials,
		ServiceName,
		ServiceNodeName,
		ServiceVersion,
		SpanCompressionEnabled,
		SpanCompressionExactMatchMaxDuration,
		SpanCompressionSameKindMaxDuration,
		SpanStackTraceMinDuration,
		StackTraceLimit,
		TraceContinuationStrategy,
		TransactionIgnoreUrls,
		TransactionMaxSpans,
		TransactionSampleRate,
		UseElasticTraceparentHeader,
		VerifyServerCert,
		ServerUrls,
		SpanFramesMinDuration,
		TraceContextIgnoreSampledFalse,
	}

	internal class ConfigurationItem
	{
		internal ConfigurationItem(ConfigurationItemId id, string environmentVariableName, string configurationKeyName)
		{
			Id = id;
			EnvironmentVariableName = environmentVariableName;
			ConfigurationKeyName = configurationKeyName;
			NormalizedName = EnvironmentVariableName.Substring(EnvVarNames.Prefix.Length).ToLower();
			NeedsMasking = Id switch
			{
				ApiKey => true,
				SecretToken => true,
				_ => false
			};
			LogAlways = Id switch
			{
				ServerUrl => true,
				ServiceName => true,
				ServiceVersion => true,
				LogLevel => true,
				_ => false
			};
		}

		public override string ToString() => $"{Id}";

		internal ConfigurationItemId Id { get; }
		internal string ConfigurationKeyName { get; }
		internal string EnvironmentVariableName { get; }
		internal string NormalizedName { get; }
		internal bool NeedsMasking { get; }
		internal bool LogAlways { get; }

		public bool IsEssentialForLogging =>
			LogAlways || Id is SecretToken or ApiKey;
	}

	internal interface IConfigurationLoggingPreambleProvider
	{
		ConfigurationKeyValue Get(ConfigurationItem item);
	}

	internal static class ConfigurationLoggingPreamble
	{
		internal static string GetDefaultValueForLogging(ConfigurationItemId configurationItemId,
			IConfigurationReader configurationReader
		) =>
			configurationItemId switch
			{
				ServerUrl => configurationReader.ServerUrl.AbsoluteUri,
				ServiceName => configurationReader.ServiceName,
				ServiceVersion => configurationReader.ServiceVersion,
				LogLevel => configurationReader.LogLevel.ToString(),
				SecretToken => configurationReader.SecretToken,
				ApiKey => configurationReader.ApiKey,
				_ => null,
			};

		internal static IEnumerable<ConfigurationItem> ConfigurationItems { get; } = new ConfigurationItem[]
		{
			new(ApiKey, EnvVarNames.ApiKey, KeyNames.ApiKey),
			new(ApplicationNamespaces, EnvVarNames.ApplicationNamespaces, KeyNames.ApplicationNamespaces),
			new(CaptureBody, EnvVarNames.CaptureBody, KeyNames.CaptureBody),
			new(CaptureBodyContentTypes, EnvVarNames.CaptureBodyContentTypes, KeyNames.CaptureBodyContentTypes),
			new(CaptureHeaders, EnvVarNames.CaptureHeaders, KeyNames.CaptureHeaders),
			new(CentralConfig, EnvVarNames.CentralConfig, KeyNames.CentralConfig),
			new(CloudProvider, EnvVarNames.CloudProvider, KeyNames.CloudProvider),
			new(DisableMetrics, EnvVarNames.DisableMetrics, KeyNames.DisableMetrics), new(Enabled, EnvVarNames.Enabled, KeyNames.Enabled),
			new(OpenTelemetryBridgeEnabled, EnvVarNames.OpenTelemetryBridgeEnabled, KeyNames.OpentelemetryBridgeEnabled),
			new(Environment, EnvVarNames.Environment, KeyNames.Environment),
			new(ExcludedNamespaces, EnvVarNames.ExcludedNamespaces, KeyNames.ExcludedNamespaces),
			new(ExitSpanMinDuration, EnvVarNames.ExitSpanMinDuration, KeyNames.ExitSpanMinDuration),
			new(FlushInterval, EnvVarNames.FlushInterval, KeyNames.FlushInterval),
			new(GlobalLabels, EnvVarNames.GlobalLabels, KeyNames.GlobalLabels), new(HostName, EnvVarNames.HostName, KeyNames.HostName),
			new(IgnoreMessageQueues, EnvVarNames.IgnoreMessageQueues, KeyNames.IgnoreMessageQueues),
			new(LogLevel, EnvVarNames.LogLevel, KeyNames.LogLevel),
			new(MaxBatchEventCount, EnvVarNames.MaxBatchEventCount, KeyNames.MaxBatchEventCount),
			new(MaxQueueEventCount, EnvVarNames.MaxQueueEventCount, KeyNames.MaxQueueEventCount),
			new(MetricsInterval, EnvVarNames.MetricsInterval, KeyNames.MetricsInterval),
			new(Recording, EnvVarNames.Recording, KeyNames.Recording),
			new(SanitizeFieldNames, EnvVarNames.SanitizeFieldNames, KeyNames.SanitizeFieldNames),
			new(SecretToken, EnvVarNames.SecretToken, KeyNames.SecretToken), new(ServerCert, EnvVarNames.ServerCert, KeyNames.ServerCert),
			new(ServerUrl, EnvVarNames.ServerUrl, KeyNames.ServerUrl),
			new(UseWindowsCredentials, EnvVarNames.UseWindowsCredentials, KeyNames.UseWindowsCredentials),
			new(ServiceName, EnvVarNames.ServiceName, KeyNames.ServiceName),
			new(ServiceNodeName, EnvVarNames.ServiceNodeName, KeyNames.ServiceNodeName),
			new(ServiceVersion, EnvVarNames.ServiceVersion, KeyNames.ServiceVersion),
			new(SpanCompressionEnabled, EnvVarNames.SpanCompressionEnabled, KeyNames.SpanCompressionEnabled),
			new(SpanCompressionExactMatchMaxDuration, EnvVarNames.SpanCompressionExactMatchMaxDuration,
				KeyNames.SpanCompressionExactMatchMaxDuration),
			new(SpanCompressionSameKindMaxDuration, EnvVarNames.SpanCompressionSameKindMaxDuration, KeyNames.SpanCompressionSameKindMaxDuration),
			new(SpanStackTraceMinDuration, EnvVarNames.SpanStackTraceMinDuration, KeyNames.SpanStackTraceMinDuration),
			new(StackTraceLimit, EnvVarNames.StackTraceLimit, KeyNames.StackTraceLimit),
			new(TraceContinuationStrategy, EnvVarNames.TraceContinuationStrategy, KeyNames.TraceContinuationStrategy),
			new(TransactionIgnoreUrls, EnvVarNames.TransactionIgnoreUrls, KeyNames.TransactionIgnoreUrls),
			new(TransactionMaxSpans, EnvVarNames.TransactionMaxSpans, KeyNames.TransactionMaxSpans),
			new(TransactionSampleRate, EnvVarNames.TransactionSampleRate, KeyNames.TransactionSampleRate),
			new(UseElasticTraceparentHeader, EnvVarNames.UseElasticTraceparentHeader, KeyNames.UseElasticTraceparentHeader),
			new(VerifyServerCert, EnvVarNames.VerifyServerCert, KeyNames.VerifyServerCert),
			new(ServerUrls, EnvVarNames.ServerUrls, KeyNames.ServerUrls),
			new(SpanFramesMinDuration, EnvVarNames.SpanFramesMinDuration, KeyNames.SpanFramesMinDuration),
			new(TraceContextIgnoreSampledFalse, EnvVarNames.TraceContextIgnoreSampledFalse, KeyNames.TraceContextIgnoreSampledFalse),
		};
	}
}
