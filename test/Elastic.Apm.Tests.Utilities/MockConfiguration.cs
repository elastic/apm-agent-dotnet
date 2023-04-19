// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Tests.Utilities
{

	public class MockConfiguration : AbstractConfigurationReader, IConfiguration, IConfigurationSnapshotDescription
	{
		public const string Origin = "unit test configuration";
		private const string ThisClassName = nameof(MockConfiguration);

		public ConfigurationKeyValue Read(string key, string value) => new(key, value, Origin);

		public MockConfiguration(IApmLogger logger = null,
			string logLevel = null,
			string serverUrls = null,
			string serviceName = null,
			string serviceVersion = null,
			string environment = null,
			string serviceNodeName = null,
			string secretToken = null,
			string apiKey = null,
			string captureHeaders = null,
			string centralConfig = null,
			string description = null,
			string openTelemetryBridgeEnabled = null,
			string exitSpanMinDuration = null,
			string transactionSampleRate = null,
			string transactionMaxSpans = null,
			string metricsInterval = null,
			string captureBody = SupportedValues.CaptureBodyOff,
			string stackTraceLimit = null,
			string spanStackTraceMinDurationInMilliseconds = null,
			string spanFramesMinDurationInMilliseconds = null,
			string captureBodyContentTypes = DefaultValues.CaptureBodyContentTypes,
			string flushInterval = null,
			string maxBatchEventCount = null,
			string maxQueueEventCount = null,
			string sanitizeFieldNames = null,
			string globalLabels = null,
			string disableMetrics = null,
			string verifyServerCert = null,
			string useElasticTraceparentHeader = null,
			string applicationNamespaces = null,
			string excludedNamespaces = null,
			string transactionIgnoreUrls = null,
			string hostName = null,
			// none is **not** the default value, but we don't want to query for cloud metadata in all tests
			string cloudProvider = SupportedValues.CloudProviderNone,
			string enabled = null,
			string recording = null,
			string serverUrl = null,
			string useWindowsCredentials = null,
			string serverCert = null,
			string ignoreMessageQueues = null,
			string traceContextIgnoreSampledFalse = null,
			string spanCompressionEnabled = null,
			string spanCompressionExactMatchMaxDuration = null,
			string spanCompressionSameKindMaxDuration = null,
			string traceContinuationStrategy = null
		) : base(logger, ThisClassName)
		{
			Description = description;

			LogLevel = ParseLogLevel(Read(EnvVarNames.LogLevel, logLevel));

			ApiKey = ParseApiKey(Read(EnvVarNames.ApiKey, apiKey));
			ApplicationNamespaces = ParseApplicationNamespaces(Read(EnvVarNames.ApplicationNamespaces, applicationNamespaces));
			CaptureBody = ParseCaptureBody(Read(EnvVarNames.CaptureBody, captureBody));
			CaptureBodyContentTypes = ParseCaptureBodyContentTypes(Read(EnvVarNames.CaptureBodyContentTypes, captureBodyContentTypes));
			CaptureHeaders = ParseCaptureHeaders(Read(EnvVarNames.CaptureHeaders, captureHeaders));
			CentralConfig = ParseCentralConfig(Read(EnvVarNames.CentralConfig, centralConfig));
			CloudProvider = ParseCloudProvider(Read(EnvVarNames.CloudProvider, cloudProvider));
			DisableMetrics = ParseDisableMetrics(Read(EnvVarNames.DisableMetrics, disableMetrics));
			Enabled = ParseEnabled(Read(EnvVarNames.Enabled, enabled));
			OpenTelemetryBridgeEnabled = ParseOpenTelemetryBridgeEnabled(Read(EnvVarNames.OpenTelemetryBridgeEnabled, openTelemetryBridgeEnabled));
			Environment = ParseEnvironment(Read(EnvVarNames.Environment, environment));
			ExcludedNamespaces = ParseExcludedNamespaces(Read(EnvVarNames.ExcludedNamespaces, excludedNamespaces));
			ExitSpanMinDuration = ParseExitSpanMinDuration(Read(EnvVarNames.ExitSpanMinDuration, exitSpanMinDuration));
			FlushInterval = ParseFlushInterval(Read(EnvVarNames.FlushInterval, flushInterval));
			GlobalLabels = ParseGlobalLabels(Read(EnvVarNames.GlobalLabels, globalLabels));
			HostName = ParseHostName(Read(EnvVarNames.HostName, hostName));
			IgnoreMessageQueues = ParseIgnoreMessageQueues(Read(EnvVarNames.IgnoreMessageQueues, ignoreMessageQueues));
			MaxBatchEventCount = ParseMaxBatchEventCount(Read(EnvVarNames.MaxBatchEventCount, maxBatchEventCount));
			MaxQueueEventCount = ParseMaxQueueEventCount(Read(EnvVarNames.MaxQueueEventCount, maxQueueEventCount));
			MetricsIntervalInMilliseconds = ParseMetricsInterval(Read(EnvVarNames.MetricsInterval, metricsInterval));
			Recording = ParseRecording(Read(EnvVarNames.Recording, recording));
			SanitizeFieldNames = ParseSanitizeFieldNames(Read(EnvVarNames.SanitizeFieldNames, sanitizeFieldNames));
			SecretToken = ParseSecretToken(Read(EnvVarNames.SecretToken, secretToken));
			ServerCert = ParseServerCert(Read(EnvVarNames.ServerCert, serverCert));
			UseWindowsCredentials = ParseUseWindowsCredentials(Read(EnvVarNames.UseWindowsCredentials, useWindowsCredentials));
			ServiceName = ParseServiceName(Read(EnvVarNames.ServiceName, serviceName));
			ServiceNodeName = ParseServiceNodeName(Read(EnvVarNames.ServiceNodeName, serviceNodeName));
			ServiceVersion = ParseServiceVersion(Read(EnvVarNames.ServiceVersion, serviceVersion));
			SpanCompressionEnabled = ParseSpanCompressionEnabled(Read(EnvVarNames.SpanCompressionEnabled, spanCompressionEnabled));
			SpanCompressionExactMatchMaxDuration =
				ParseSpanCompressionExactMatchMaxDuration(Read(EnvVarNames.SpanCompressionExactMatchMaxDuration, spanCompressionExactMatchMaxDuration));
			SpanCompressionSameKindMaxDuration =
				ParseSpanCompressionSameKindMaxDuration(Read(EnvVarNames.SpanCompressionSameKindMaxDuration, spanCompressionSameKindMaxDuration));
#pragma warning disable CS0618
			SpanFramesMinDurationInMilliseconds = ParseSpanFramesMinDurationInMilliseconds(Read(EnvVarNames.SpanFramesMinDuration, spanFramesMinDurationInMilliseconds));
#pragma warning restore CS0618
			SpanStackTraceMinDurationInMilliseconds =
				ParseSpanStackTraceMinDurationInMilliseconds(Read(EnvVarNames.SpanStackTraceMinDuration, spanStackTraceMinDurationInMilliseconds));
			StackTraceLimit = ParseStackTraceLimit(Read(EnvVarNames.StackTraceLimit, stackTraceLimit));
			TraceContextIgnoreSampledFalse =
				ParseTraceContextIgnoreSampledFalse(Read(EnvVarNames.TraceContextIgnoreSampledFalse, traceContextIgnoreSampledFalse));
			TraceContinuationStrategy = ParseTraceContinuationStrategy(Read(EnvVarNames.TraceContinuationStrategy, traceContinuationStrategy));
			TransactionIgnoreUrls =
				ParseTransactionIgnoreUrls(Read(EnvVarNames.TransactionIgnoreUrls, transactionIgnoreUrls));
			TransactionMaxSpans = ParseTransactionMaxSpans(Read(EnvVarNames.TransactionMaxSpans, transactionMaxSpans));
			TransactionSampleRate = ParseTransactionSampleRate(Read(EnvVarNames.TransactionSampleRate, transactionSampleRate));
			UseElasticTraceparentHeader = ParseUseElasticTraceparentHeader(Read(EnvVarNames.UseElasticTraceparentHeader, useElasticTraceparentHeader));
			VerifyServerCert = ParseVerifyServerCert(Read(EnvVarNames.VerifyServerCert, verifyServerCert));

			var urls = Read(EnvVarNames.ServerUrls, serverUrls);
			var url = Read(EnvVarNames.ServerUrl, serverUrl);
#pragma warning disable CS0618
			ServerUrls = ParseServerUrls(!string.IsNullOrEmpty(urls.Value) ? urls : url);
			ServerUrl = !string.IsNullOrEmpty(url.Value) ? ParseServerUrl(url) : ServerUrls.FirstOrDefault();
#pragma warning restore CS0618
		}

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
		public IReadOnlyList<Uri> ServerUrls { get; }
		public bool UseWindowsCredentials { get; }
		public string ServiceName { get; }
		public string ServiceNodeName { get; }
		public string ServiceVersion { get; }
		public bool SpanCompressionEnabled { get; }
		public double SpanCompressionExactMatchMaxDuration { get; }
		public double SpanCompressionSameKindMaxDuration { get; }
		public double SpanStackTraceMinDurationInMilliseconds { get; }
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
