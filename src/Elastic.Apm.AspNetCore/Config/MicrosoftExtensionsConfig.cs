using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore.Config
{
	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback
	/// </summary>
	internal class MicrosoftExtensionsConfig : AbstractConfigurationReader, IConfigurationReader
	{
		internal const string Origin = "Microsoft.Extensions.Configuration";
		private const string ThisClassName = nameof(MicrosoftExtensionsConfig);

		private readonly IConfiguration _configuration;
		private readonly string _environmentName;

		private new readonly IApmLogger _logger;

		private readonly Lazy<double> _spanFramesMinDurationInMilliseconds;

		private readonly Lazy<int> _stackTraceLimit;

		public MicrosoftExtensionsConfig(IConfiguration configuration, IApmLogger logger, string environmentName)
			: base(logger, ThisClassName)
		{
			_logger = logger?.Scoped(ThisClassName);

			_configuration = configuration;
			_environmentName = environmentName;
			_configuration.GetSection("ElasticApm")
				?
				.GetReloadToken()
				.RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));

			_stackTraceLimit =
				new Lazy<int>(() => ParseStackTraceLimit(ReadFallBack(Keys.StackTraceLimit, ConfigConsts.EnvVarNames.StackTraceLimit)));

			_spanFramesMinDurationInMilliseconds = new Lazy<double>(() =>
				ParseSpanFramesMinDurationInMilliseconds(ReadFallBack(Keys.SpanFramesMinDuration, ConfigConsts.EnvVarNames.SpanFramesMinDuration)));
		}

		internal static class Keys
		{
			internal const string CaptureBody = "ElasticApm:CaptureBody";
			internal const string CaptureBodyContentTypes = "ElasticApm:CaptureBodyContentTypes";
			internal const string CaptureHeaders = "ElasticApm:CaptureHeaders";
			internal const string CentralConfig = "ElasticApm:CentralConfig";
			internal const string Environment = "ElasticApm:Environment";
			internal const string FlushInterval = "ElasticApm:FlushInterval";
			internal const string LogLevel = "ElasticApm:LogLevel";
			internal const string LogLevelSubKey = "LogLevel";
			internal const string MaxBatchEventCount = "ElasticApm:MaxBatchEventCount";
			internal const string MaxQueueEventCount = "ElasticApm:MaxQueueEventCount";
			internal const string MetricsInterval = "ElasticApm:MetricsInterval";
			internal const string SecretToken = "ElasticApm:SecretToken";
			internal const string ServerUrls = "ElasticApm:ServerUrls";
			internal const string ServiceName = "ElasticApm:ServiceName";
			internal const string ServiceVersion = "ElasticApm:ServiceVersion";
			internal const string SpanFramesMinDuration = "ElasticApm:SpanFramesMinDuration";
			internal const string StackTraceLimit = "ElasticApm:StackTraceLimit";
			internal const string TransactionSampleRate = "ElasticApm:TransactionSampleRate";
		}


		private LogLevel? _logLevel;

		public string CaptureBody => ParseCaptureBody(ReadFallBack(Keys.CaptureBody, ConfigConsts.EnvVarNames.CaptureBody));

		public List<string> CaptureBodyContentTypes =>
			ParseCaptureBodyContentTypes(ReadFallBack(Keys.CaptureBodyContentTypes, ConfigConsts.EnvVarNames.CaptureBodyContentTypes), CaptureBody);

		public bool CaptureHeaders => ParseCaptureHeaders(ReadFallBack(Keys.CaptureHeaders, ConfigConsts.EnvVarNames.CaptureHeaders));

		public bool CentralConfig => ParseCentralConfig(ReadFallBack(Keys.CentralConfig, ConfigConsts.EnvVarNames.CentralConfig));

		public string Environment => ParseEnvironment(ReadFallBack(Keys.Environment, ConfigConsts.EnvVarNames.Environment)) ?? _environmentName;

		public TimeSpan FlushInterval => ParseFlushInterval(ReadFallBack(Keys.FlushInterval, ConfigConsts.EnvVarNames.FlushInterval));

		public LogLevel LogLevel
		{
			get
			{
				if (_logLevel.HasValue) return _logLevel.Value;

				var l = ParseLogLevel(ReadFallBack(Keys.LogLevel, ConfigConsts.EnvVarNames.LogLevel));
				_logLevel = l;
				return l;
			}
		}

		public int MaxBatchEventCount => ParseMaxBatchEventCount(ReadFallBack(Keys.MaxBatchEventCount, ConfigConsts.EnvVarNames.MaxBatchEventCount));

		public int MaxQueueEventCount => ParseMaxQueueEventCount(ReadFallBack(Keys.MaxQueueEventCount, ConfigConsts.EnvVarNames.MaxQueueEventCount));

		public double MetricsIntervalInMilliseconds =>
			ParseMetricsInterval(ReadFallBack(Keys.MetricsInterval, ConfigConsts.EnvVarNames.MetricsInterval));

		public string SecretToken => ParseSecretToken(ReadFallBack(Keys.SecretToken, ConfigConsts.EnvVarNames.SecretToken));

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(ReadFallBack(Keys.ServerUrls, ConfigConsts.EnvVarNames.ServerUrls));

		public string ServiceName => ParseServiceName(ReadFallBack(Keys.ServiceName, ConfigConsts.EnvVarNames.ServiceName));

		public string ServiceVersion => ParseServiceVersion(ReadFallBack(Keys.ServiceVersion, ConfigConsts.EnvVarNames.ServiceVersion));

		public double SpanFramesMinDurationInMilliseconds => _spanFramesMinDurationInMilliseconds.Value;

		public int StackTraceLimit => _stackTraceLimit.Value;

		public double TransactionSampleRate =>
			ParseTransactionSampleRate(ReadFallBack(Keys.TransactionSampleRate, ConfigConsts.EnvVarNames.TransactionSampleRate));

		private ConfigurationKeyValue Read(string key) => Kv(key, _configuration[key], Origin);

		private ConfigurationKeyValue ReadFallBack(string key, string fallBackEnvVarName)
		{
			var primary = Read(key);
			if (!string.IsNullOrWhiteSpace(primary.Value)) return primary;

			var secondary = Kv(fallBackEnvVarName, _configuration[fallBackEnvVarName], EnvironmentConfigurationReader.Origin);
			return secondary;
		}

		private void ChangeCallback(object obj)
		{
			if (!(obj is IConfigurationSection section)) return;

			var newLogLevel = ParseLogLevel(Kv(Keys.LogLevelSubKey, section[Keys.LogLevelSubKey], Origin));
			if (_logLevel.HasValue && newLogLevel == _logLevel.Value) return;

			_logLevel = newLogLevel;
			_logger.Info()?.Log("Updated log level to {LogLevel}", newLogLevel);
		}
	}
}
