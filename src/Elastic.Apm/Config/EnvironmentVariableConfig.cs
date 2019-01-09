using System;

namespace Elastic.Apm.Config
{
	internal class EnvironmentVariableConfig : AbstractAgentConfig
	{
		protected override (string value, string configType, string configKey) ReadServerUrls() => (
			Environment.GetEnvironmentVariable(EnvVarConsts.ServerUrls), "environment variable", EnvVarConsts.ServerUrls);

		protected override (string value, string configType, string configKey) ReadLogLevel() => (
			Environment.GetEnvironmentVariable(EnvVarConsts.LogLevel), "environment variable", EnvVarConsts.LogLevel);
	}

	internal static class EnvVarConsts
	{
		public static string LogLevel => "ELASTIC_APM_LOG_LEVEL";
		public static string ServerUrls => "ELASTIC_APM_SERVER_URLS";
	}
}
