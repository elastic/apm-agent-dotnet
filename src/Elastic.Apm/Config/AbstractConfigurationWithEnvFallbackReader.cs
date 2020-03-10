using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

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
				new Lazy<int>(() => ParseStackTraceLimit(Read(ConfigConsts.KeyNames.StackTraceLimit, ConfigConsts.EnvVarNames.StackTraceLimit)));

			_spanFramesMinDurationInMilliseconds = new Lazy<double>(() =>
				ParseSpanFramesMinDurationInMilliseconds(Read(ConfigConsts.KeyNames.SpanFramesMinDuration,
					ConfigConsts.EnvVarNames.SpanFramesMinDuration)));
		}

		protected abstract ConfigurationKeyValue Read(string key, string fallBackEnvVarName);

		public virtual string CaptureBody => ParseCaptureBody(Read(ConfigConsts.KeyNames.CaptureBody, ConfigConsts.EnvVarNames.CaptureBody));

		public virtual List<string> CaptureBodyContentTypes =>
			ParseCaptureBodyContentTypes(Read(ConfigConsts.KeyNames.CaptureBodyContentTypes, ConfigConsts.EnvVarNames.CaptureBodyContentTypes));

		public virtual bool CaptureHeaders =>
			ParseCaptureHeaders(Read(ConfigConsts.KeyNames.CaptureHeaders, ConfigConsts.EnvVarNames.CaptureHeaders));

		public bool CentralConfig => ParseCentralConfig(Read(ConfigConsts.KeyNames.CentralConfig, ConfigConsts.EnvVarNames.CentralConfig));

		public IReadOnlyList<WildcardMatcher> DisableMetrics =>
			ParseDisableMetrics(Read(ConfigConsts.KeyNames.DisableMetrics, ConfigConsts.EnvVarNames.DisableMetrics));

		public virtual string Environment => ParseEnvironment(Read(ConfigConsts.KeyNames.Environment, ConfigConsts.EnvVarNames.Environment))
			?? _defaultEnvironmentName;

		public virtual TimeSpan FlushInterval =>
			ParseFlushInterval(Read(ConfigConsts.KeyNames.FlushInterval, ConfigConsts.EnvVarNames.FlushInterval));

		public IReadOnlyDictionary<string, string> GlobalLabels =>
			ParseGlobalLabels(Read(ConfigConsts.KeyNames.GlobalLabels, ConfigConsts.EnvVarNames.GlobalLabels));

		public virtual LogLevel LogLevel => ParseLogLevel(Read(ConfigConsts.KeyNames.LogLevel, ConfigConsts.EnvVarNames.LogLevel));

		public virtual int MaxBatchEventCount =>
			ParseMaxBatchEventCount(Read(ConfigConsts.KeyNames.MaxBatchEventCount, ConfigConsts.EnvVarNames.MaxBatchEventCount));

		public virtual int MaxQueueEventCount =>
			ParseMaxQueueEventCount(Read(ConfigConsts.KeyNames.MaxQueueEventCount, ConfigConsts.EnvVarNames.MaxQueueEventCount));

		public virtual double MetricsIntervalInMilliseconds =>
			ParseMetricsInterval(Read(ConfigConsts.KeyNames.MetricsInterval, ConfigConsts.EnvVarNames.MetricsInterval));

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames =>
			ParseSanitizeFieldNames(Read(ConfigConsts.KeyNames.SanitizeFieldNames, ConfigConsts.EnvVarNames.SanitizeFieldNames));

		public virtual string SecretToken => ParseSecretToken(Read(ConfigConsts.KeyNames.SecretToken, ConfigConsts.EnvVarNames.SecretToken));
		public string ApiKey => ParseApiKey(Read(ConfigConsts.KeyNames.ApiKey, ConfigConsts.EnvVarNames.ApiKey));

		public virtual IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Read(ConfigConsts.KeyNames.ServerUrls, ConfigConsts.EnvVarNames.ServerUrls));

		public virtual string ServiceName => ParseServiceName(Read(ConfigConsts.KeyNames.ServiceName, ConfigConsts.EnvVarNames.ServiceName));

		public string ServiceNodeName => ParseServiceNodeName(Read(ConfigConsts.KeyNames.ServiceNodeName, ConfigConsts.EnvVarNames.ServiceNodeName));

		public virtual string ServiceVersion =>
			ParseServiceVersion(Read(ConfigConsts.KeyNames.ServiceVersion, ConfigConsts.EnvVarNames.ServiceVersion));

		public virtual double SpanFramesMinDurationInMilliseconds => _spanFramesMinDurationInMilliseconds.Value;

		public virtual int StackTraceLimit => _stackTraceLimit.Value;

		public virtual int TransactionMaxSpans =>
			ParseTransactionMaxSpans(Read(ConfigConsts.KeyNames.TransactionMaxSpans, ConfigConsts.EnvVarNames.TransactionMaxSpans));

		public virtual double TransactionSampleRate =>
			ParseTransactionSampleRate(Read(ConfigConsts.KeyNames.TransactionSampleRate, ConfigConsts.EnvVarNames.TransactionSampleRate));

		public bool UseElasticTraceparentHeader => ParseUseElasticTraceparentHeader(Read(ConfigConsts.KeyNames.UseElasticTraceparentheader,
			ConfigConsts.EnvVarNames.UseElasticTraceparentHeader));

		public virtual bool VerifyServerCert =>
			ParseVerifyServerCert(Read(ConfigConsts.KeyNames.VerifyServerCert, ConfigConsts.EnvVarNames.VerifyServerCert));
	}
}
