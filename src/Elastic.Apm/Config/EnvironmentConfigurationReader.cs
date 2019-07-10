using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class EnvironmentConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		internal const string Origin = "environment";

		public EnvironmentConfigurationReader(IApmLogger logger = null) : base(logger) { }

		public LogLevel LogLevel => ParseLogLevel(Read(ConfigConsts.EnvVarNames.LogLevel));

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Read(ConfigConsts.EnvVarNames.ServerUrls));

		public string SecretToken => ParseSecretToken(Read(ConfigConsts.EnvVarNames.SecretToken));

		public string ServiceName => ParseServiceName(Read(ConfigConsts.EnvVarNames.ServiceName));

		public bool CaptureHeaders => ParseCaptureHeaders(Read(ConfigConsts.EnvVarNames.CaptureHeaders));

		public double TransactionSampleRate => ParseTransactionSampleRate(Read(ConfigConsts.EnvVarNames.TransactionSampleRate));

		public double MetricsIntervalInMillisecond => ParseMetricsInterval(Read(ConfigConsts.EnvVarNames.MetricsInterval));

		public int StackTraceLimit => ParseStackTraceLimit(Read(ConfigConsts.EnvVarNames.StackTraceLimit));

		public double SpanFramesMinDurationInMilliseconds => ParseSpanFramesMinDurationInMilliseconds(Read(ConfigConsts.EnvVarNames.StackTraceLimit));

		private static ConfigurationKeyValue Read(string key) =>
			new ConfigurationKeyValue(key, Environment.GetEnvironmentVariable(key), Origin);
	}
}
