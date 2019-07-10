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
		private readonly string _secretToken;
		private readonly string _captureHeaders;
		private readonly string _transactionSampleRate;
		private readonly string _metricsInterval;
		private readonly string _stackTraceLimit;
		private readonly string _spanFramesMinDurationInMilliseconds;

		public TestAgentConfigurationReader(
			IApmLogger logger,
			string logLevel = null,
			string serverUrls = null,
			string serviceName = null,
			string secretToken = null,
			string captureHeaders = null,
			string transactionSampleRate = null,
			string metricsInterval = null,
			string stackTraceLimit = null,
			string spanFramesMinDurationInMilliseconds = null
		) : base(logger)
		{
			Logger = logger ?? new TestLogger();
			_serverUrls = serverUrls;
			_logLevel = logLevel;
			_serviceName = serviceName;
			_secretToken = secretToken;
			_captureHeaders = captureHeaders;
			_transactionSampleRate = transactionSampleRate;
			_metricsInterval = metricsInterval;
			_stackTraceLimit = stackTraceLimit;
			_spanFramesMinDurationInMilliseconds = spanFramesMinDurationInMilliseconds;
		}

		public new IApmLogger Logger { get; }

		public LogLevel LogLevel => ParseLogLevel(Kv(ConfigConsts.EnvVarNames.LogLevel, _logLevel, Origin));
		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Kv(ConfigConsts.EnvVarNames.ServerUrls, _serverUrls, Origin));
		public string ServiceName => ParseServiceName(Kv(ConfigConsts.EnvVarNames.ServiceName, _serviceName, Origin));
		public string SecretToken => ParseSecretToken(Kv(ConfigConsts.EnvVarNames.SecretToken, _secretToken, Origin));
		public bool CaptureHeaders => ParseCaptureHeaders(Kv(ConfigConsts.EnvVarNames.CaptureHeaders, _captureHeaders, Origin));

		public double TransactionSampleRate =>
			ParseTransactionSampleRate(Kv(ConfigConsts.EnvVarNames.TransactionSampleRate, _transactionSampleRate, Origin));

		public double MetricsIntervalInMillisecond => ParseMetricsInterval(Kv(ConfigConsts.EnvVarNames.MetricsInterval, _metricsInterval, Origin));
		public int StackTraceLimit => ParseStackTraceLimit(Kv(ConfigConsts.EnvVarNames.StackTraceLimit, _stackTraceLimit, Origin));

		public double SpanFramesMinDurationInMilliseconds => ParseSpanFramesMinDurationInMilliseconds(Kv(
			ConfigConsts.EnvVarNames.SpanFramesMinDuration,
			_spanFramesMinDurationInMilliseconds, Origin));
	}
}
