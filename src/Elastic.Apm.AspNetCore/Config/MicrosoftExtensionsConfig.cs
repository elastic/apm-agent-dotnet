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
        private readonly IConfiguration configuration;

        public MicrosoftExtensionsConfig(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        protected override (string value, string configType, string configKey) ReadServerUrls()
        {
            var configValue = configuration[MicrosoftExtensionConfigConsts.ServerUrls];
            return String.IsNullOrEmpty(configValue) ? (configuration[EnvVarConsts.ServerUrls], "environment variable", EnvVarConsts.ServerUrls) : (configValue, "IConfiguration", MicrosoftExtensionConfigConsts.ServerUrls);
        }

        protected override (string value, string configType, string configKey) ReadLogLevel()
        {
            var configValue = configuration[MicrosoftExtensionConfigConsts.LogLevel];
            return String.IsNullOrEmpty(configValue) ? (configuration[EnvVarConsts.LogLevel], "environment variable", EnvVarConsts.LogLevel) : (configValue, "IConfiguration", MicrosoftExtensionConfigConsts.LogLevel);
        }
    }

    internal static class MicrosoftExtensionConfigConsts
    {
        public static string ServerUrls => "ElasticApm:ServerUrls";
        public static string LogLevel => "ElasticApm:LogLevel";
    }
}
