using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class MockConfigSnapshot : AbstractConfigurationReader, IConfigSnapshot
	{
		public const string Origin = "unit test configuration";
		private const string ThisClassName = nameof(MockConfigSnapshot);

		private readonly string _captureBody;
		private readonly string _captureBodyContentTypes;
		private readonly string _captureHeaders;
		private readonly string _centralConfig;
		private readonly string _dbgDescription;
		private readonly string _environment;
		private readonly string _flushInterval;
		private readonly string _globalLabels;
		private readonly string _logLevel;
		private readonly string _maxBatchEventCount;
		private readonly string _maxQueueEventCount;
		private readonly string _metricsInterval;
		private readonly string _sanitizeFieldNames;
		private readonly string _secretToken;
		private readonly string _serverUrls;
		private readonly string _serviceName;
		private readonly string _serviceVersion;
		private readonly string _spanFramesMinDurationInMilliseconds;
		private readonly string _stackTraceLimit;
		private readonly string _transactionMaxSpans;
		private readonly string _transactionSampleRate;

		public MockConfigSnapshot(
			IApmLogger logger = null,
			string logLevel = null,
			string serverUrls = null,
			string serviceName = null,
			string serviceVersion = null,
			string environment = null,
			string secretToken = null,
			string captureHeaders = null,
			string centralConfig = null,
			string dbgDescription = null,
			string transactionSampleRate = null,
			string transactionMaxSpans = null,
			string metricsInterval = null,
			string captureBody = ConfigConsts.SupportedValues.CaptureBodyOff,
			string stackTraceLimit = null,
			string spanFramesMinDurationInMilliseconds = null,
			string captureBodyContentTypes = ConfigConsts.DefaultValues.CaptureBodyContentTypes,
			string flushInterval = null,
			string maxBatchEventCount = null,
			string maxQueueEventCount = null,
			string sanitizeFieldNames = null,
			string globalLabels = null
		) : base(logger, ThisClassName)
		{
			_serverUrls = serverUrls;
			_logLevel = logLevel;
			_serviceName = serviceName;
			_serviceVersion = serviceVersion;
			_environment = environment;
			_secretToken = secretToken;
			_captureHeaders = captureHeaders;
			_centralConfig = centralConfig;
			_dbgDescription = dbgDescription;
			_transactionSampleRate = transactionSampleRate;
			_transactionMaxSpans = transactionMaxSpans;
			_metricsInterval = metricsInterval;
			_captureBody = captureBody;
			_stackTraceLimit = stackTraceLimit;
			_spanFramesMinDurationInMilliseconds = spanFramesMinDurationInMilliseconds;
			_captureBodyContentTypes = captureBodyContentTypes;
			_flushInterval = flushInterval;
			_maxBatchEventCount = maxBatchEventCount;
			_maxQueueEventCount = maxQueueEventCount;
			_sanitizeFieldNames = sanitizeFieldNames;
			_globalLabels = globalLabels;
		}

		public string CaptureBody => ParseCaptureBody(Kv(ConfigConsts.EnvVarNames.CaptureBody, _captureBody, Origin));

		public List<string> CaptureBodyContentTypes =>
			ParseCaptureBodyContentTypes(Kv(ConfigConsts.EnvVarNames.CaptureBodyContentTypes, _captureBodyContentTypes, Origin), CaptureBody);

		public bool CaptureHeaders => ParseCaptureHeaders(Kv(ConfigConsts.EnvVarNames.CaptureHeaders, _captureHeaders, Origin));
		public bool CentralConfig => ParseCentralConfig(Kv(ConfigConsts.EnvVarNames.CentralConfig, _centralConfig, Origin));

		public string DbgDescription => _dbgDescription ?? nameof(MockConfigSnapshot);
		public string Environment => ParseEnvironment(Kv(ConfigConsts.EnvVarNames.Environment, _environment, Origin));

		public TimeSpan FlushInterval => ParseFlushInterval(Kv(ConfigConsts.EnvVarNames.FlushInterval, _flushInterval, Origin));

		public IReadOnlyDictionary<string, string> GlobalLabels =>
			ParseGlobalLabels(Kv(ConfigConsts.EnvVarNames.GlobalLabels, _globalLabels, Origin));

		public LogLevel LogLevel => ParseLogLevel(Kv(ConfigConsts.EnvVarNames.LogLevel, _logLevel, Origin));
		public int MaxBatchEventCount => ParseMaxBatchEventCount(Kv(ConfigConsts.EnvVarNames.MaxBatchEventCount, _maxBatchEventCount, Origin));
		public int MaxQueueEventCount => ParseMaxQueueEventCount(Kv(ConfigConsts.EnvVarNames.MaxQueueEventCount, _maxQueueEventCount, Origin));
		public double MetricsIntervalInMilliseconds => ParseMetricsInterval(Kv(ConfigConsts.EnvVarNames.MetricsInterval, _metricsInterval, Origin));

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames =>
			ParseSanitizeFieldNames(Kv(ConfigConsts.EnvVarNames.SanitizeFieldNames, _sanitizeFieldNames, Origin));

		public string SecretToken => ParseSecretToken(Kv(ConfigConsts.EnvVarNames.SecretToken, _secretToken, Origin));
		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Kv(ConfigConsts.EnvVarNames.ServerUrls, _serverUrls, Origin));
		public string ServiceName => ParseServiceName(Kv(ConfigConsts.EnvVarNames.ServiceName, _serviceName, Origin));
		public string ServiceVersion => ParseServiceVersion(Kv(ConfigConsts.EnvVarNames.ServiceVersion, _serviceVersion, Origin));

		public double SpanFramesMinDurationInMilliseconds => ParseSpanFramesMinDurationInMilliseconds(Kv(
			ConfigConsts.EnvVarNames.SpanFramesMinDuration,
			_spanFramesMinDurationInMilliseconds, Origin));

		public int StackTraceLimit => ParseStackTraceLimit(Kv(ConfigConsts.EnvVarNames.StackTraceLimit, _stackTraceLimit, Origin));

		public int TransactionMaxSpans => ParseTransactionMaxSpans(Kv(ConfigConsts.EnvVarNames.TransactionMaxSpans, _transactionMaxSpans, Origin));

		public double TransactionSampleRate =>
			ParseTransactionSampleRate(Kv(ConfigConsts.EnvVarNames.TransactionSampleRate, _transactionSampleRate, Origin));
	}
}
