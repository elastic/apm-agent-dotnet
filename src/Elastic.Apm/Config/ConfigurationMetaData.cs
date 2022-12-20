// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;

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
		EnableOpenTelemetryBridge,
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
			NormalizedName = EnvironmentVariableName.Substring(ConfigConsts.EnvVarNames.Prefix.Length).ToLower();
			NeedsMasking = Id switch
			{
				ConfigurationItemId.ApiKey => true,
				ConfigurationItemId.SecretToken => true,
				_ => false
			};
			LogAlways = Id switch
			{
				ConfigurationItemId.ServerUrl => true,
				ConfigurationItemId.ServiceName => true,
				ConfigurationItemId.ServiceVersion => true,
				ConfigurationItemId.LogLevel => true,
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
			LogAlways || Id is ConfigurationItemId.SecretToken or ConfigurationItemId.ApiKey;
	}

	internal interface IConfigurationMetaDataProvider
	{
		ConfigurationKeyValue Get(ConfigurationItem item);
	}

	internal static class ConfigurationMetaData
	{
		internal static string GetDefaultValueForLogging(ConfigurationItemId configurationItemId,
			IConfigurationReader configurationReader) =>
			configurationItemId switch
			{
				ConfigurationItemId.ServerUrl => configurationReader.ServerUrl.AbsoluteUri,
				ConfigurationItemId.ServiceName => configurationReader.ServiceName,
				ConfigurationItemId.ServiceVersion => configurationReader.ServiceVersion,
				ConfigurationItemId.LogLevel => configurationReader.LogLevel.ToString(),
				ConfigurationItemId.SecretToken => configurationReader.SecretToken,
				ConfigurationItemId.ApiKey => configurationReader.ApiKey,
				_ => null,
			};

		internal static IEnumerable<ConfigurationItem> ConfigurationItems { get; } = new ConfigurationItem[]
		{
			new(ConfigurationItemId.ApiKey, ConfigConsts.EnvVarNames.ApiKey, ConfigConsts.KeyNames.ApiKey), new(
				ConfigurationItemId.ApplicationNamespaces, ConfigConsts.EnvVarNames.ApplicationNamespaces,
				ConfigConsts.KeyNames.ApplicationNamespaces),
			new(ConfigurationItemId.CaptureBody, ConfigConsts.EnvVarNames.CaptureBody,
				ConfigConsts.KeyNames.CaptureBody),
			new(ConfigurationItemId.CaptureBodyContentTypes, ConfigConsts.EnvVarNames.CaptureBodyContentTypes,
				ConfigConsts.KeyNames.CaptureBodyContentTypes),
			new(ConfigurationItemId.CaptureHeaders, ConfigConsts.EnvVarNames.CaptureHeaders,
				ConfigConsts.KeyNames.CaptureHeaders),
			new(ConfigurationItemId.CentralConfig, ConfigConsts.EnvVarNames.CentralConfig,
				ConfigConsts.KeyNames.CentralConfig),
			new(ConfigurationItemId.CloudProvider, ConfigConsts.EnvVarNames.CloudProvider,
				ConfigConsts.KeyNames.CloudProvider),
			new(ConfigurationItemId.DisableMetrics, ConfigConsts.EnvVarNames.DisableMetrics,
				ConfigConsts.KeyNames.DisableMetrics),
			new(ConfigurationItemId.Enabled, ConfigConsts.EnvVarNames.Enabled, ConfigConsts.KeyNames.Enabled), new(
				ConfigurationItemId.EnableOpenTelemetryBridge, ConfigConsts.EnvVarNames.EnableOpenTelemetryBridge,
				ConfigConsts.KeyNames.EnableOpenTelemetryBridge),
			new(ConfigurationItemId.Environment, ConfigConsts.EnvVarNames.Environment,
				ConfigConsts.KeyNames.Environment),
			new(ConfigurationItemId.ExcludedNamespaces, ConfigConsts.EnvVarNames.ExcludedNamespaces,
				ConfigConsts.KeyNames.ExcludedNamespaces),
			new(ConfigurationItemId.ExitSpanMinDuration, ConfigConsts.EnvVarNames.ExitSpanMinDuration,
				ConfigConsts.KeyNames.ExitSpanMinDuration),
			new(ConfigurationItemId.FlushInterval, ConfigConsts.EnvVarNames.FlushInterval,
				ConfigConsts.KeyNames.FlushInterval),
			new(ConfigurationItemId.GlobalLabels, ConfigConsts.EnvVarNames.GlobalLabels,
				ConfigConsts.KeyNames.GlobalLabels),
			new(ConfigurationItemId.HostName, ConfigConsts.EnvVarNames.HostName, ConfigConsts.KeyNames.HostName), new(
				ConfigurationItemId.IgnoreMessageQueues, ConfigConsts.EnvVarNames.IgnoreMessageQueues,
				ConfigConsts.KeyNames.IgnoreMessageQueues),
			new(ConfigurationItemId.LogLevel, ConfigConsts.EnvVarNames.LogLevel, ConfigConsts.KeyNames.LogLevel), new(
				ConfigurationItemId.MaxBatchEventCount, ConfigConsts.EnvVarNames.MaxBatchEventCount,
				ConfigConsts.KeyNames.MaxBatchEventCount),
			new(ConfigurationItemId.MaxQueueEventCount, ConfigConsts.EnvVarNames.MaxQueueEventCount,
				ConfigConsts.KeyNames.MaxQueueEventCount),
			new(ConfigurationItemId.MetricsInterval, ConfigConsts.EnvVarNames.MetricsInterval,
				ConfigConsts.KeyNames.MetricsInterval),
			new(ConfigurationItemId.Recording, ConfigConsts.EnvVarNames.Recording, ConfigConsts.KeyNames.Recording),
			new(ConfigurationItemId.SanitizeFieldNames, ConfigConsts.EnvVarNames.SanitizeFieldNames,
				ConfigConsts.KeyNames.SanitizeFieldNames),
			new(ConfigurationItemId.SecretToken, ConfigConsts.EnvVarNames.SecretToken,
				ConfigConsts.KeyNames.SecretToken),
			new(ConfigurationItemId.ServerCert, ConfigConsts.EnvVarNames.ServerCert,
				ConfigConsts.KeyNames.ServerCert),
			new(ConfigurationItemId.ServerUrl, ConfigConsts.EnvVarNames.ServerUrl, ConfigConsts.KeyNames.ServerUrl),
			new(ConfigurationItemId.UseWindowsCredentials, ConfigConsts.EnvVarNames.UseWindowsCredentials,
				ConfigConsts.KeyNames.UseWindowsCredentials),
			new(ConfigurationItemId.ServiceName, ConfigConsts.EnvVarNames.ServiceName,
				ConfigConsts.KeyNames.ServiceName),
			new(ConfigurationItemId.ServiceNodeName, ConfigConsts.EnvVarNames.ServiceNodeName,
				ConfigConsts.KeyNames.ServiceNodeName),
			new(ConfigurationItemId.ServiceVersion, ConfigConsts.EnvVarNames.ServiceVersion,
				ConfigConsts.KeyNames.ServiceVersion),
			new(ConfigurationItemId.SpanCompressionEnabled, ConfigConsts.EnvVarNames.SpanCompressionEnabled,
				ConfigConsts.KeyNames.SpanCompressionEnabled),
			new(ConfigurationItemId.SpanCompressionExactMatchMaxDuration,
				ConfigConsts.EnvVarNames.SpanCompressionExactMatchMaxDuration,
				ConfigConsts.KeyNames.SpanCompressionExactMatchMaxDuration),
			new(ConfigurationItemId.SpanCompressionSameKindMaxDuration,
				ConfigConsts.EnvVarNames.SpanCompressionSameKindMaxDuration,
				ConfigConsts.KeyNames.SpanCompressionSameKindMaxDuration),
			new(ConfigurationItemId.SpanStackTraceMinDuration,
				ConfigConsts.EnvVarNames.SpanStackTraceMinDuration,
				ConfigConsts.KeyNames.SpanStackTraceMinDuration),
			new(ConfigurationItemId.StackTraceLimit, ConfigConsts.EnvVarNames.StackTraceLimit,
				ConfigConsts.KeyNames.StackTraceLimit),
			new(ConfigurationItemId.TraceContinuationStrategy, ConfigConsts.EnvVarNames.TraceContinuationStrategy,
				ConfigConsts.KeyNames.TraceContinuationStrategy),
			new(ConfigurationItemId.TransactionIgnoreUrls, ConfigConsts.EnvVarNames.TransactionIgnoreUrls,
				ConfigConsts.KeyNames.TransactionIgnoreUrls),
			new(ConfigurationItemId.TransactionMaxSpans, ConfigConsts.EnvVarNames.TransactionMaxSpans,
				ConfigConsts.KeyNames.TransactionMaxSpans),
			new(ConfigurationItemId.TransactionSampleRate, ConfigConsts.EnvVarNames.TransactionSampleRate,
				ConfigConsts.KeyNames.TransactionSampleRate),
			new(ConfigurationItemId.UseElasticTraceparentHeader,
				ConfigConsts.EnvVarNames.UseElasticTraceparentHeader,
				ConfigConsts.KeyNames.UseElasticTraceparentHeader),
			new(ConfigurationItemId.VerifyServerCert, ConfigConsts.EnvVarNames.VerifyServerCert,
				ConfigConsts.KeyNames.VerifyServerCert),
			new(ConfigurationItemId.ServerUrls, ConfigConsts.EnvVarNames.ServerUrls,
				ConfigConsts.KeyNames.ServerUrls),
			new(ConfigurationItemId.SpanFramesMinDuration,
				ConfigConsts.EnvVarNames.SpanFramesMinDuration, ConfigConsts.KeyNames.SpanFramesMinDuration),
			new(ConfigurationItemId.TraceContextIgnoreSampledFalse,
				ConfigConsts.EnvVarNames.TraceContextIgnoreSampledFalse,
				ConfigConsts.KeyNames.TraceContextIgnoreSampledFalse),
		};
	}
}
