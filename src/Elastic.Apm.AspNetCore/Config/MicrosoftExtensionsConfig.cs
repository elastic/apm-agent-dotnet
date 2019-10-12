using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore.Config
{
	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback
	/// </summary>
	internal class MicrosoftExtensionsConfig : AbstractConfigurationWithEnvFallbackReader
	{
		private const string LogLevelSubKey = "LogLevel";
		internal const string Origin = "Microsoft.Extensions.Configuration";
		private const string ThisClassName = nameof(MicrosoftExtensionsConfig);
		private readonly IConfiguration _configuration;

		private readonly IApmLogger _logger;

		public MicrosoftExtensionsConfig(IConfiguration configuration, IApmLogger logger, string defaultEnvironmentName)
			: base(logger, defaultEnvironmentName, ThisClassName)
		{
			_logger = logger?.Scoped(ThisClassName);

			_configuration = configuration;
			_configuration.GetSection("ElasticApm")
				?
				.GetReloadToken()
				.RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));
		}

		private LogLevel? _logLevel;

		public override LogLevel LogLevel
		{
			get
			{
				if (_logLevel.HasValue) return _logLevel.Value;

				var l = ParseLogLevel(Read(ConfigConsts.KeyNames.LogLevel, ConfigConsts.EnvVarNames.LogLevel));
				_logLevel = l;
				return l;
			}
		}

		protected override ConfigurationKeyValue Read(string key, string fallBackEnvVarName)
		{
			var value = _configuration[key];
			if (!string.IsNullOrWhiteSpace(value)) return Kv(key, value, Origin);

			var secondary = Kv(fallBackEnvVarName, ReadEnvVarValue(fallBackEnvVarName), EnvironmentConfigurationReader.Origin);
			return secondary;
		}

		private void ChangeCallback(object obj)
		{
			if (!(obj is IConfigurationSection section)) return;

			var newLogLevel = ParseLogLevel(Kv(LogLevelSubKey, section[LogLevelSubKey], Origin));
			if (_logLevel.HasValue && newLogLevel == _logLevel.Value) return;

			_logLevel = newLogLevel;
			_logger.Info()?.Log("Updated log level to {LogLevel}", newLogLevel);
		}
	}
}
