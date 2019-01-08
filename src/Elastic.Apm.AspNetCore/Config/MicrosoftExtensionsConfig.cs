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
    public class MicrosoftExtensionsConfig : AbstractAgentConfig
    {
        private readonly IConfiguration _configuration;

        public MicrosoftExtensionsConfig(IConfiguration configuration)
        {
            this._configuration = configuration;
            this._configuration.GetSection("ElasticApm")?
                .GetReloadToken().RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));
        }


        protected override (string value, string configType, string configKey) ReadServerUrls()
        {
            var configValue = _configuration[MicrosoftExtensionConfigConsts.ServerUrls];
            return String.IsNullOrEmpty(configValue) ? (_configuration[EnvVarConsts.ServerUrls], "environment variable", EnvVarConsts.ServerUrls) : (configValue, "IConfiguration", MicrosoftExtensionConfigConsts.ServerUrls);
        }

        protected override (string value, string configType, string configKey) ReadLogLevel()
        {
            var configValue = _configuration[MicrosoftExtensionConfigConsts.LogLevel];
            return String.IsNullOrEmpty(configValue) ? (_configuration[EnvVarConsts.LogLevel], "environment variable", EnvVarConsts.LogLevel) : (configValue, "IConfiguration", MicrosoftExtensionConfigConsts.LogLevel);
        }

        private void ChangeCallback(Object obj)
        {
            (var newlogLevel, var isError)
                = ParseLogLevel((obj as IConfigurationSection)?[MicrosoftExtensionConfigConsts.LogLevel.Split(':')[1]]);

            if(!isError && newlogLevel.HasValue && newlogLevel.Value != logLevel)
            {
                logLevel = newlogLevel;
                Logger?.LogInfo($"Updated log level to {logLevel}");
            }

            if(isError)
            {
                Logger?.LogInfo($"Updating log level failed, current log level: {logLevel}");
            }
        }
    }

    internal static class MicrosoftExtensionConfigConsts
    {
        public static string ServerUrls => "ElasticApm:ServerUrls";
        public static string LogLevel => "ElasticApm:LogLevel";
    }
}
