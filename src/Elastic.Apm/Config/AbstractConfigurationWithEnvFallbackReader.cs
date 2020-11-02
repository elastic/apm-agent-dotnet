// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Config
{
	internal abstract class AbstractConfigurationWithEnvFallbackReader : AbstractConfigurationReader, IConfigurationReader
	{
		private readonly string _defaultEnvironmentName;

		private readonly Lazy<double> _spanFramesMinDurationInMilliseconds;

		private readonly Lazy<int> _stackTraceLimit;

		internal AbstractConfigurationWithEnvFallbackReader(IApmLogger logger, string defaultEnvironmentName, string dbgDerivedClassName)
			: base(logger, dbgDerivedClassName)
		{
			_defaultEnvironmentName = defaultEnvironmentName;

			_stackTraceLimit =
				new Lazy<int>(() => ParseStackTraceLimit(Read(KeyNames.StackTraceLimit, EnvVarNames.StackTraceLimit)));

			_spanFramesMinDurationInMilliseconds = new Lazy<double>(() =>
				ParseSpanFramesMinDurationInMilliseconds(Read(KeyNames.SpanFramesMinDuration,
					EnvVarNames.SpanFramesMinDuration)));
		}

		protected abstract ConfigurationKeyValue Read(string key, string fallBackEnvVarName);

		public virtual string CaptureBody => ParseCaptureBody(Read(KeyNames.CaptureBody, EnvVarNames.CaptureBody));

		public virtual List<string> CaptureBodyContentTypes =>
			ParseCaptureBodyContentTypes(Read(KeyNames.CaptureBodyContentTypes, EnvVarNames.CaptureBodyContentTypes));

		public virtual bool CaptureHeaders =>
			ParseCaptureHeaders(Read(KeyNames.CaptureHeaders, EnvVarNames.CaptureHeaders));

		public bool CentralConfig => ParseCentralConfig(Read(KeyNames.CentralConfig, EnvVarNames.CentralConfig));

		public virtual string CloudProvider => ParseCloudProvider(Read(KeyNames.CloudProvider, EnvVarNames.CloudProvider));

		public IReadOnlyList<WildcardMatcher> DisableMetrics =>
			ParseDisableMetrics(Read(KeyNames.DisableMetrics, EnvVarNames.DisableMetrics));

		public virtual string Environment => ParseEnvironment(Read(KeyNames.Environment, EnvVarNames.Environment))
			?? _defaultEnvironmentName;

		public virtual TimeSpan FlushInterval =>
			ParseFlushInterval(Read(KeyNames.FlushInterval, EnvVarNames.FlushInterval));

		public IReadOnlyDictionary<string, string> GlobalLabels =>
			ParseGlobalLabels(Read(KeyNames.GlobalLabels, EnvVarNames.GlobalLabels));

		public virtual string HostName => ParseHostName(Read(KeyNames.HostName, EnvVarNames.HostName));

		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls =>
			ParseTransactionIgnoreUrls(Read(KeyNames.TransactionIgnoreUrls, EnvVarNames.TransactionIgnoreUrls));

		public virtual LogLevel LogLevel => ParseLogLevel(Read(KeyNames.LogLevel, EnvVarNames.LogLevel));

		public virtual int MaxBatchEventCount =>
			ParseMaxBatchEventCount(Read(KeyNames.MaxBatchEventCount, EnvVarNames.MaxBatchEventCount));

		public virtual int MaxQueueEventCount =>
			ParseMaxQueueEventCount(Read(KeyNames.MaxQueueEventCount, EnvVarNames.MaxQueueEventCount));

		public virtual double MetricsIntervalInMilliseconds =>
			ParseMetricsInterval(Read(KeyNames.MetricsInterval, EnvVarNames.MetricsInterval));

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames =>
			ParseSanitizeFieldNames(Read(KeyNames.SanitizeFieldNames, EnvVarNames.SanitizeFieldNames));

		public virtual string SecretToken => ParseSecretToken(Read(KeyNames.SecretToken, EnvVarNames.SecretToken));
		public string ApiKey => ParseApiKey(Read(KeyNames.ApiKey, EnvVarNames.ApiKey));

		public virtual IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Read(KeyNames.ServerUrls, EnvVarNames.ServerUrls));

		public virtual string ServiceName => ParseServiceName(Read(KeyNames.ServiceName, EnvVarNames.ServiceName));

		public string ServiceNodeName => ParseServiceNodeName(Read(KeyNames.ServiceNodeName, EnvVarNames.ServiceNodeName));

		public virtual string ServiceVersion =>
			ParseServiceVersion(Read(KeyNames.ServiceVersion, EnvVarNames.ServiceVersion));

		public virtual double SpanFramesMinDurationInMilliseconds => _spanFramesMinDurationInMilliseconds.Value;

		public virtual int StackTraceLimit => _stackTraceLimit.Value;

		public virtual int TransactionMaxSpans =>
			ParseTransactionMaxSpans(Read(KeyNames.TransactionMaxSpans, EnvVarNames.TransactionMaxSpans));

		public virtual double TransactionSampleRate =>
			ParseTransactionSampleRate(Read(KeyNames.TransactionSampleRate, EnvVarNames.TransactionSampleRate));

		public bool UseElasticTraceparentHeader => ParseUseElasticTraceparentHeader(Read(KeyNames.UseElasticTraceparentHeader,
			EnvVarNames.UseElasticTraceparentHeader));

		public virtual bool VerifyServerCert =>
			ParseVerifyServerCert(Read(KeyNames.VerifyServerCert, EnvVarNames.VerifyServerCert));

		public IReadOnlyCollection<string> ExcludedNamespaces =>
			ParseExcludedNamespaces(Read(KeyNames.ExcludedNamespaces, EnvVarNames.ExcludedNamespaces));

		public IReadOnlyCollection<string> ApplicationNamespaces =>
			ParseExcludedNamespaces(Read(KeyNames.ApplicationNamespaces, EnvVarNames.ApplicationNamespaces));
	}
}
