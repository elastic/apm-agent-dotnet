using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class EnvironmentConfigurationReader : AbstractConfigurationReader, IConfigSnapshot
	{
		internal const string Origin = "environment variables";
		private const string ThisClassName = nameof(EnvironmentConfigurationReader);

		private readonly Lazy<double> _spanFramesMinDurationInMilliseconds;

		private readonly Lazy<int> _stackTraceLimit;

		public EnvironmentConfigurationReader(IApmLogger logger = null) : base(logger, ThisClassName)
		{
			_spanFramesMinDurationInMilliseconds
				= new Lazy<double>(() =>
					ParseSpanFramesMinDurationInMilliseconds(Read(ConfigConsts.EnvVarNames.SpanFramesMinDuration)));

			_stackTraceLimit = new Lazy<int>(() => ParseStackTraceLimit(Read(ConfigConsts.EnvVarNames.StackTraceLimit)));
		}

		public string CaptureBody => ParseCaptureBody(Read(ConfigConsts.EnvVarNames.CaptureBody));

		public List<string> CaptureBodyContentTypes => ParseCaptureBodyContentTypes(Read(ConfigConsts.EnvVarNames.CaptureBodyContentTypes));

		public bool CaptureHeaders => ParseCaptureHeaders(Read(ConfigConsts.EnvVarNames.CaptureHeaders));

		public bool CentralConfig => ParseCentralConfig(Read(ConfigConsts.EnvVarNames.CentralConfig));

		public string DbgDescription => Origin;
		public IReadOnlyList<WildcardMatcher> DisableMetrics => ParseDisableMetrics(Read(ConfigConsts.EnvVarNames.DisableMetrics));

		public string Environment => ParseEnvironment(Read(ConfigConsts.EnvVarNames.Environment));

		public TimeSpan FlushInterval => ParseFlushInterval(Read(ConfigConsts.EnvVarNames.FlushInterval));

		public IReadOnlyDictionary<string, string> GlobalLabels => ParseGlobalLabels(Read(ConfigConsts.EnvVarNames.GlobalLabels));

		public LogLevel LogLevel => ParseLogLevel(Read(ConfigConsts.EnvVarNames.LogLevel));

		public int MaxBatchEventCount => ParseMaxBatchEventCount(Read(ConfigConsts.EnvVarNames.MaxBatchEventCount));

		public int MaxQueueEventCount => ParseMaxQueueEventCount(Read(ConfigConsts.EnvVarNames.MaxQueueEventCount));

		public double MetricsIntervalInMilliseconds => ParseMetricsInterval(Read(ConfigConsts.EnvVarNames.MetricsInterval));

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => ParseSanitizeFieldNames(Read(ConfigConsts.EnvVarNames.SanitizeFieldNames));

		public string SecretToken => ParseSecretToken(Read(ConfigConsts.EnvVarNames.SecretToken));

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Read(ConfigConsts.EnvVarNames.ServerUrls));

		public string ServiceName => ParseServiceName(Read(ConfigConsts.EnvVarNames.ServiceName));

		public string ServiceNodeName => ParseServiceNodeName(Read(ConfigConsts.EnvVarNames.ServiceNodeName));

		public string ServiceVersion => ParseServiceVersion(Read(ConfigConsts.EnvVarNames.ServiceVersion));

		public double SpanFramesMinDurationInMilliseconds => _spanFramesMinDurationInMilliseconds.Value;

		public int StackTraceLimit => _stackTraceLimit.Value;

		public int TransactionMaxSpans => ParseTransactionMaxSpans(Read(ConfigConsts.EnvVarNames.TransactionMaxSpans));

		public double TransactionSampleRate => ParseTransactionSampleRate(Read(ConfigConsts.EnvVarNames.TransactionSampleRate));

		public bool UseElasticTraceparentHeader => ParseUseElasticTraceparentHeader(Read(ConfigConsts.EnvVarNames.UseElasticTraceparentHeader));

		private ConfigurationKeyValue Read(string key) =>
			new ConfigurationKeyValue(key, ReadEnvVarValue(key), Origin);
	}
}
