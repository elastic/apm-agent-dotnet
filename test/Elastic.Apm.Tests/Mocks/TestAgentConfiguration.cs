using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestAgentConfiguration : AbstractAgentConfig
	{
		private readonly string _serverUrls;
		private readonly string _logLevel;

		public TestAgentConfiguration(
			string serverUrls = null,
			string logLevel = "Debug",
			AbstractLogger logger = null,
			Service service = null,
			IPayloadSender payloadSender = null
		) : base(logger ?? new TestLogger(LogLevelOrDefault(logLevel)), service, payloadSender ?? new MockPayloadSender())
		{
			_serverUrls = serverUrls;
			_logLevel = logLevel;
		}

		protected override (string value, string configType, string configKey) ReadLogLevel() => (_logLevel, "test", EnvVarConsts.LogLevel);

		protected override (string value, string configType, string configKey) ReadServerUrls() => (_serverUrls, "test", EnvVarConsts.ServerUrls);
	}
}
