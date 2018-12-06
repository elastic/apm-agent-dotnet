using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
    internal class EnvironmentVariableConfig : AbstractAgentConfig
    {
        protected override (string value, string configType, string configKey) ReadServerUrls() => (Environment.GetEnvironmentVariable(EnvVarConsts.ServerUrls), "environment variable", EnvVarConsts.ServerUrls);
        protected override (string value, string configType, string configKey) ReadLogLevel() => (Environment.GetEnvironmentVariable(EnvVarConsts.LogLevel), "environment variable", EnvVarConsts.LogLevel);
    }

    internal static class EnvVarConsts
    {
        public static String ServerUrls => "ELASTIC_APM_SERVER_URLS";
        public static String LogLevel => "ELASTIC_APM_LOG_LEVEL";
    }
}
