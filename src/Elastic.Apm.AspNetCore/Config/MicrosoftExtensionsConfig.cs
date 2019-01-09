using Elastic.Apm.Config;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore.Config
{
	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback
	/// </summary>
	public class MicrosoftExtensionsConfig : AbstractAgentConfig
	{
		private readonly IConfiguration _configuration;

		public MicrosoftExtensionsConfig(IConfiguration configuration)
		{
			this._configuration = configuration;
			this._configuration.GetSection("ElasticApm")
				?
				.GetReloadToken()
				.RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));
		}


		protected override (string value, string configType, string configKey) ReadServerUrls()
		{
			var configValue = _configuration[MicrosoftExtensionConfigConsts.ServerUrls];
			return string.IsNullOrEmpty(configValue)
				? (_configuration[EnvVarConsts.ServerUrls], "environment variable", EnvVarConsts.ServerUrls)
				: (configValue, "IConfiguration", MicrosoftExtensionConfigConsts.ServerUrls);
		}

		protected override (string value, string configType, string configKey) ReadLogLevel()
		{
			var configValue = _configuration[MicrosoftExtensionConfigConsts.LogLevel];
			return string.IsNullOrEmpty(configValue)
				? (_configuration[EnvVarConsts.LogLevel], "environment variable", EnvVarConsts.LogLevel)
				: (configValue, "IConfiguration", MicrosoftExtensionConfigConsts.LogLevel);
		}

		private void ChangeCallback(object obj)
		{
			var (newLogLevel, isError)
				= ParseLogLevel((obj as IConfigurationSection)?[MicrosoftExtensionConfigConsts.LogLevel.Split(':')[1]]);

			if (!isError && newLogLevel.HasValue && newLogLevel.Value != LogLevelBackingField)
			{
				LogLevelBackingField = newLogLevel;
				Logger?.LogInfo($"Updated log level to {LogLevelBackingField.ToString()}");
			}

			if (isError) Logger?.LogInfo($"Updating log level failed, current log level: {LogLevelBackingField.ToString()}");
		}
	}

	internal static class MicrosoftExtensionConfigConsts
	{
		public static string LogLevel => "ElasticApm:LogLevel";
		public static string ServerUrls => "ElasticApm:ServerUrls";
	}
}
