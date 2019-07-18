using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class EnvironmentConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		internal const string Origin = "environment";

		private readonly Lazy<double> _spanFramesMinDurationInMilliseconds;

		private readonly Lazy<int> _stackTraceLimit;

		public EnvironmentConfigurationReader(IApmLogger logger = null) : base(logger)
		{
			_spanFramesMinDurationInMilliseconds
				= new Lazy<double>(() =>
					ParseSpanFramesMinDurationInMilliseconds(Read(ConfigConsts.EnvVarNames.StackTraceLimit)));

			_stackTraceLimit = new Lazy<int>(() => ParseStackTraceLimit(Read(ConfigConsts.EnvVarNames.StackTraceLimit)));
		}

		public bool CaptureHeaders => ParseCaptureHeaders(Read(ConfigConsts.EnvVarNames.CaptureHeaders));

		public LogLevel LogLevel => ParseLogLevel(Read(ConfigConsts.EnvVarNames.LogLevel));

		public double MetricsIntervalInMilliseconds => ParseMetricsInterval(Read(ConfigConsts.EnvVarNames.MetricsInterval));

		public string SecretToken => ParseSecretToken(Read(ConfigConsts.EnvVarNames.SecretToken));

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Read(ConfigConsts.EnvVarNames.ServerUrls));

		public string ServiceName => ParseServiceName(Read(ConfigConsts.EnvVarNames.ServiceName));

		public double SpanFramesMinDurationInMilliseconds => _spanFramesMinDurationInMilliseconds.Value;

		public int StackTraceLimit => _stackTraceLimit.Value;

		public double TransactionSampleRate => ParseTransactionSampleRate(Read(ConfigConsts.EnvVarNames.TransactionSampleRate));

		private static ConfigurationKeyValue Read(string key) =>
			new ConfigurationKeyValue(key, Environment.GetEnvironmentVariable(key), Origin);
	}
}
