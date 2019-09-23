using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore.Config
{
	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback
	/// </summary>
	internal class MicrosoftExtensionsConfig : AbstractConfigurationWithEnvFallbackReader, IConfigurationReader
	{
		internal const string Origin = "Microsoft.Extensions.Configuration";

		private readonly IConfiguration _configuration;

		public MicrosoftExtensionsConfig(IConfiguration configuration, IApmLogger logger, string environmentName) : base(logger, environmentName)
		{
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

			var secondary = Kv(fallBackEnvVarName, _configuration[fallBackEnvVarName], EnvironmentConfigurationReader.Origin);
			return secondary;
		}

		private void ChangeCallback(object obj)
		{
			if (!(obj is IConfigurationSection section)) return;

			var newLogLevel = ParseLogLevel(Kv(ConfigConsts.KeyNames.LogLevelSubKey, section[ConfigConsts.KeyNames.LogLevelSubKey], Origin));
			if (_logLevel.HasValue && newLogLevel == _logLevel.Value) return;

			_logLevel = newLogLevel;
			Logger.Info()?.Log("Updated log level to {LogLevel}", newLogLevel);
		}
	}
}
