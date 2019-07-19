using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestAgentConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		public const string Origin = "unit test configuration";

		private readonly string _logLevel;
		private readonly string _serverUrls;
		private readonly string _serviceName;
		private readonly string _serviceVersion;
		private readonly string _secretToken;
		private readonly string _captureHeaders;
		private readonly string _transactionSampleRate;
		private readonly string _metricsInterval;
		private readonly string _captureBody;
		private readonly string _stackTraceLimit;
		private readonly string _spanFramesMinDurationInMilliseconds;
		private readonly string _captureBodyContentTypes;

		public TestAgentConfigurationReader(
			IApmLogger logger = null,
			string logLevel = null,
			string serverUrls = null,
			string serviceName = null,
			string serviceVersion = null,
			string secretToken = null,
			string captureHeaders = null,
			string transactionSampleRate = null,
			string metricsInterval = null,
			string captureBody = ConfigConsts.SupportedValues.CaptureBodyOff,
			string stackTraceLimit = null,
			string spanFramesMinDurationInMilliseconds = null,
			string captureBodyContentTypes = ConfigConsts.DefaultValues.CaptureBodyContentTypes
		) : base(logger)
		{
			Logger = logger ?? new TestLogger();
			_serverUrls = serverUrls;
			_logLevel = logLevel;
			_serviceName = serviceName;
			_serviceVersion = serviceVersion;
			_secretToken = secretToken;
			_captureHeaders = captureHeaders;
			_transactionSampleRate = transactionSampleRate;
			_metricsInterval = metricsInterval;
			_captureBody = captureBody;
			_stackTraceLimit = stackTraceLimit;
			_spanFramesMinDurationInMilliseconds = spanFramesMinDurationInMilliseconds;
			_captureBodyContentTypes = captureBodyContentTypes;
		}

		public new IApmLogger Logger { get; }
		public LogLevel LogLevel => ParseLogLevel(Kv(ConfigConsts.EnvVarNames.LogLevel, _logLevel, Origin));
		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Kv(ConfigConsts.EnvVarNames.ServerUrls, _serverUrls, Origin));
		public string ServiceName => ParseServiceName(Kv(ConfigConsts.EnvVarNames.ServiceName, _serviceName, Origin));
		public string ServiceVersion => ParseServiceVersion(Kv(ConfigConsts.EnvVarNames.ServiceVersion, _serviceVersion,  Origin));
		public string SecretToken => ParseSecretToken(Kv(ConfigConsts.EnvVarNames.SecretToken, _secretToken, Origin));
		public bool CaptureHeaders => ParseCaptureHeaders(Kv(ConfigConsts.EnvVarNames.CaptureHeaders, _captureHeaders, Origin));
		public double TransactionSampleRate => ParseTransactionSampleRate(Kv(ConfigConsts.EnvVarNames.TransactionSampleRate, _transactionSampleRate, Origin));
		public string CaptureBody => ParseCaptureBody(Kv(ConfigConsts.EnvVarNames.CaptureBody, _captureBody, Origin));
		public double MetricsIntervalInMilliseconds => ParseMetricsInterval(Kv(ConfigConsts.EnvVarNames.MetricsInterval, _metricsInterval, Origin));
		public int StackTraceLimit => ParseStackTraceLimit(Kv(ConfigConsts.EnvVarNames.StackTraceLimit, _stackTraceLimit, Origin));

		public double SpanFramesMinDurationInMilliseconds => ParseSpanFramesMinDurationInMilliseconds(Kv(
			ConfigConsts.EnvVarNames.SpanFramesMinDuration,
			_spanFramesMinDurationInMilliseconds, Origin));

		public List<string> CaptureBodyContentTypes => ParseCaptureBodyContentTypes(Kv(ConfigConsts.EnvVarNames.CaptureBodyContentTypes, _captureBodyContentTypes, Origin), CaptureBody);
	}
}
