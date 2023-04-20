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
	internal interface IConfigurationKeyValueProvider
	{
		ConfigurationKeyValue Read(string key);
	}

	internal abstract class FallbackToEnvironmentConfigurationBase : AbstractConfigurationReader, IConfigurationReader, IConfigurationLoggingPreambleProvider
	{
		internal FallbackToEnvironmentConfigurationBase(IApmLogger logger, string defaultEnvironmentName, string dbgDerivedClassName, IConfigurationKeyValueProvider configKeyValueProvider)
			: base(logger, dbgDerivedClassName)
		{
			ActiveConfiguration = configKeyValueProvider;
			EnvironmentConfiguration = new EnvironmentKeyValueProvider();

			LogLevel = ParseLogLevel(Read(KeyNames.LogLevel, EnvVarNames.LogLevel));

			Environment = ParseEnvironment(Read(KeyNames.Environment, EnvVarNames.Environment)) ?? defaultEnvironmentName;

			ApiKey = ParseApiKey(Read(KeyNames.ApiKey, EnvVarNames.ApiKey));
			ApplicationNamespaces = ParseApplicationNamespaces(Read(KeyNames.ApplicationNamespaces, EnvVarNames.ApplicationNamespaces));
			CaptureBody = ParseCaptureBody(Read(KeyNames.CaptureBody, EnvVarNames.CaptureBody));
			CaptureBodyContentTypes = ParseCaptureBodyContentTypes(Read(KeyNames.CaptureBodyContentTypes, EnvVarNames.CaptureBodyContentTypes));
			CaptureHeaders = ParseCaptureHeaders(Read(KeyNames.CaptureHeaders, EnvVarNames.CaptureHeaders));
			CentralConfig = ParseCentralConfig(Read(KeyNames.CentralConfig, EnvVarNames.CentralConfig));
			CloudProvider = ParseCloudProvider(Read(KeyNames.CloudProvider, EnvVarNames.CloudProvider));
			DisableMetrics = ParseDisableMetrics(Read(KeyNames.ApiKey, EnvVarNames.ApiKey));
			Enabled = ParseEnabled(Read(KeyNames.Enabled, EnvVarNames.Enabled));
			OpenTelemetryBridgeEnabled = ParseOpenTelemetryBridgeEnabled(Read(KeyNames.OpentelemetryBridgeEnabled, EnvVarNames.OpenTelemetryBridgeEnabled));
			ExcludedNamespaces = ParseExcludedNamespaces(Read(KeyNames.ExcludedNamespaces, EnvVarNames.ExcludedNamespaces));
			ExitSpanMinDuration = ParseExitSpanMinDuration(Read(KeyNames.ExitSpanMinDuration, EnvVarNames.ExitSpanMinDuration));
			FlushInterval = ParseFlushInterval(Read(KeyNames.FlushInterval, EnvVarNames.FlushInterval));
			GlobalLabels = ParseGlobalLabels(Read(KeyNames.GlobalLabels, EnvVarNames.GlobalLabels));
			HostName = ParseHostName(Read(KeyNames.HostName, EnvVarNames.HostName));
			IgnoreMessageQueues = ParseIgnoreMessageQueues(Read(KeyNames.IgnoreMessageQueues, EnvVarNames.IgnoreMessageQueues));
			MaxBatchEventCount = ParseMaxBatchEventCount(Read(KeyNames.MaxBatchEventCount, EnvVarNames.MaxBatchEventCount));
			MaxQueueEventCount = ParseMaxQueueEventCount(Read(KeyNames.MaxQueueEventCount, EnvVarNames.MaxQueueEventCount));
			MetricsIntervalInMilliseconds = ParseMetricsInterval(Read(KeyNames.MetricsInterval, EnvVarNames.MetricsInterval));
			Recording = ParseRecording(Read(KeyNames.Recording, EnvVarNames.Recording));
			SanitizeFieldNames = ParseSanitizeFieldNames(Read(KeyNames.SanitizeFieldNames, EnvVarNames.SanitizeFieldNames));
			SecretToken = ParseSecretToken(Read(KeyNames.SecretToken, EnvVarNames.SecretToken));
			ServerCert = ParseServerCert(Read(KeyNames.ServerCert, EnvVarNames.ServerCert));
			UseWindowsCredentials = ParseUseWindowsCredentials(Read(KeyNames.UseWindowsCredentials, EnvVarNames.UseWindowsCredentials));
			ServiceName = ParseServiceName(Read(KeyNames.ServiceName, EnvVarNames.ServiceName));
			ServiceNodeName = ParseServiceNodeName(Read(KeyNames.ServiceNodeName, EnvVarNames.ServiceNodeName));
			ServiceVersion = ParseServiceVersion(Read(KeyNames.ServiceVersion, EnvVarNames.ServiceVersion));
			SpanCompressionEnabled = ParseSpanCompressionEnabled(Read(KeyNames.SpanCompressionEnabled, EnvVarNames.SpanCompressionEnabled));
			SpanCompressionExactMatchMaxDuration =
				ParseSpanCompressionExactMatchMaxDuration(Read(KeyNames.SpanCompressionExactMatchMaxDuration, EnvVarNames.SpanCompressionExactMatchMaxDuration));
			SpanCompressionSameKindMaxDuration =
				ParseSpanCompressionSameKindMaxDuration(Read(KeyNames.SpanCompressionSameKindMaxDuration, EnvVarNames.SpanCompressionSameKindMaxDuration));
#pragma warning disable CS0618
			SpanFramesMinDurationInMilliseconds = ParseSpanFramesMinDurationInMilliseconds(Read(KeyNames.SpanFramesMinDuration, EnvVarNames.SpanFramesMinDuration));
#pragma warning restore CS0618
			SpanStackTraceMinDurationInMilliseconds =
				ParseSpanStackTraceMinDurationInMilliseconds(Read(KeyNames.SpanStackTraceMinDuration, EnvVarNames.SpanStackTraceMinDuration));
			StackTraceLimit = ParseStackTraceLimit(Read(KeyNames.StackTraceLimit, EnvVarNames.StackTraceLimit));
			TraceContextIgnoreSampledFalse =
				ParseTraceContextIgnoreSampledFalse(Read(KeyNames.TraceContextIgnoreSampledFalse, EnvVarNames.TraceContextIgnoreSampledFalse));
			TraceContinuationStrategy = ParseTraceContinuationStrategy(Read(KeyNames.TraceContinuationStrategy, EnvVarNames.TraceContinuationStrategy));
			TransactionIgnoreUrls =
				ParseTransactionIgnoreUrls(Read(KeyNames.TransactionIgnoreUrls, EnvVarNames.TransactionIgnoreUrls));
			TransactionMaxSpans = ParseTransactionMaxSpans(Read(KeyNames.TransactionMaxSpans, EnvVarNames.TransactionMaxSpans));
			TransactionSampleRate = ParseTransactionSampleRate(Read(KeyNames.TransactionSampleRate, EnvVarNames.TransactionSampleRate));
			UseElasticTraceparentHeader = ParseUseElasticTraceparentHeader(Read(KeyNames.UseElasticTraceparentHeader, EnvVarNames.UseElasticTraceparentHeader));
			VerifyServerCert = ParseVerifyServerCert(Read(KeyNames.VerifyServerCert, EnvVarNames.VerifyServerCert));

			var urlConfig = Read(KeyNames.ServerUrl, EnvVarNames.ServerUrl);
			var urlsConfig = Read(KeyNames.ServerUrls, EnvVarNames.ServerUrls);
#pragma warning disable CS0618
			ServerUrls = ParseServerUrls(!string.IsNullOrEmpty(urlsConfig.Value) ? urlsConfig : urlConfig);
			ServerUrl = !string.IsNullOrEmpty(urlConfig.Value) ? ParseServerUrl(urlConfig) : ServerUrls.FirstOrDefault();
#pragma warning restore CS0618
		}

		private IConfigurationKeyValueProvider ActiveConfiguration { get; }

		protected EnvironmentKeyValueProvider EnvironmentConfiguration { get; }

		public ConfigurationKeyValue Get(ConfigurationItem item) => ActiveConfiguration.Read(item.ConfigurationKeyName) ?? EnvironmentConfiguration.Read(item.EnvironmentVariableName);

		protected ConfigurationKeyValue Read(string key, string envFallback) => ActiveConfiguration.Read(key) ?? EnvironmentConfiguration.Read(envFallback);

		public string ApiKey { get;  }

		public IReadOnlyCollection<string> ApplicationNamespaces { get;  }

		public string CaptureBody { get;  }

		public List<string> CaptureBodyContentTypes { get;  }

		public bool CaptureHeaders { get;  }

		public bool CentralConfig { get;  }

		public string CloudProvider { get;  }

		public IReadOnlyList<WildcardMatcher> DisableMetrics { get;  }

		public bool Enabled { get;  }

		public string Environment { get;  }

		public IReadOnlyCollection<string> ExcludedNamespaces { get;  }

		public double ExitSpanMinDuration { get;  }

		public TimeSpan FlushInterval { get;  }

		public IReadOnlyDictionary<string, string> GlobalLabels { get;  }

		public string HostName { get;  }

		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues { get;  }

		public LogLevel LogLevel { get; protected set; }

		public int MaxBatchEventCount { get;  }

		public int MaxQueueEventCount { get;  }

		public double MetricsIntervalInMilliseconds { get;  }

		public bool Recording { get;  }

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get;  }

		public string SecretToken { get;  }

		public string ServerCert { get;  }

		/// <inheritdoc />
		public Uri ServerUrl { get; }

		/// <inheritdoc />
		[Obsolete("Use ServerUrl")]
		public IReadOnlyList<Uri> ServerUrls { get;  }

		public bool UseWindowsCredentials { get;  }

		public string ServiceName { get; protected set; }

		public string ServiceNodeName { get;  }

		public string ServiceVersion { get;  }

		public bool SpanCompressionEnabled { get;  }

		public double SpanCompressionExactMatchMaxDuration { get;  }

		public double SpanCompressionSameKindMaxDuration { get;  }

		public double SpanStackTraceMinDurationInMilliseconds { get;  }

		[Obsolete("Use SpanStackTraceMinDurationInMilliseconds")]
		public double SpanFramesMinDurationInMilliseconds { get;  }

		public int StackTraceLimit { get;  }

		public bool TraceContextIgnoreSampledFalse { get;  }

		public string TraceContinuationStrategy { get;  }

		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls { get;  }

		public int TransactionMaxSpans { get;  }

		public double TransactionSampleRate { get;  }

		public bool UseElasticTraceparentHeader { get;  }

		public bool VerifyServerCert { get;  }

		public bool OpenTelemetryBridgeEnabled { get;  }
	}
}
