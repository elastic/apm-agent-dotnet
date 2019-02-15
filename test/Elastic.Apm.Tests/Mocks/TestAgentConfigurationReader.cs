using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestAgentConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		public const string Origin = "unit test configuration";

		private readonly string _logLevel;
		private readonly string _serverUrls;
		private readonly string _serviceName;
		private readonly string _secretToken;

		public TestAgentConfigurationReader(
			AbstractLogger logger,
			string serverUrls = null,
			string logLevel = "Debug",
			string serviceName = null,
			string secretToken = null
		) : base(logger)
		{
			Logger = logger;
			_serverUrls = serverUrls;
			_logLevel = logLevel;
			_serviceName = serviceName;
			_secretToken = secretToken;
		}

		public new AbstractLogger Logger { get; }

		public LogLevel LogLevel => ParseLogLevel(Kv(ConfigConsts.ConfigKeys.Level, _logLevel, Origin));
		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Kv(ConfigConsts.ConfigKeys.Urls, _serverUrls, Origin));
		public string ServiceName => ParseServiceName(Kv(ConfigConsts.ConfigKeys.ServiceName, _serviceName, Origin));
		public string SecretToken => ParseSecretToken(Kv(ConfigConsts.ConfigKeys.SecretToken, _secretToken, Origin));
	}
}
