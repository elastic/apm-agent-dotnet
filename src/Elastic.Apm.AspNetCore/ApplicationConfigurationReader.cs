using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore
{
	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback.
	/// </summary>
	internal class ApplicationConfigurationReader : AbstractConfigurationWithEnvFallbackReader
	{
		private const string LogLevelSubKey = "LogLevel";
		internal const string Origin = "Microsoft.Extensions.Configuration";
		private LogLevel? _logLevel;
		private readonly IConfiguration _configuration;
		private readonly IApmLogger _logger;

		public ApplicationConfigurationReader(IConfiguration configuration, IApmLogger logger, string environmentName)
			: base(logger, environmentName, nameof(ApplicationConfigurationReader))
		{
			_logger = logger?.Scoped(nameof(ApplicationConfigurationReader));

			_configuration = configuration;
			_configuration.GetSection("ElasticApm")?.GetReloadToken().RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));
		}

		public override LogLevel LogLevel
		{
			get
			{
				if (_logLevel.HasValue) return _logLevel.Value;

				_logLevel = ParseLogLevel(Read(ConfigConsts.KeyNames.LogLevel, ConfigConsts.EnvVarNames.LogLevel));
				return _logLevel.GetValueOrDefault();
			}
		}

		protected override ConfigurationKeyValue Read(string key, string fallBackEnvVarName)
		{
			var value = _configuration[key];
			return value != null ? Kv(key, value, Origin) : Kv(fallBackEnvVarName, ReadEnvVarValue(fallBackEnvVarName), EnvironmentConfigurationReader.Origin);
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
