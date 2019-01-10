using System;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Config
{
	internal class EnvironmentVariableConfig : AbstractAgentConfig
	{
		public EnvironmentVariableConfig(AbstractLogger logger = null, Service service = null, IPayloadSender sender = null)
			: base(logger, service, sender) { }

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
