using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestAgentConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		private readonly string _logLevel;
		private readonly string _serverUrls;

		public const string Origin = "unit test configuration";

		internal static (string Level, string Urls) Keys = (
			Level: "ELASTIC_APM_LOG_LEVEL",
			Urls: "ELASTIC_APM_SERVER_URLS"
		);

		public TestAgentConfigurationReader(
			AbstractLogger logger,
			string serverUrls = null,
			string logLevel = "Debug"
		) : base(logger)
		{
			Logger = logger;
			_serverUrls = serverUrls;
			_logLevel = logLevel;
		}

		public new AbstractLogger Logger { get; }

		public LogLevel LogLevel => ParseLogLevel(Kv(Keys.Level, _logLevel, Origin));
		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Kv(Keys.Urls, _serverUrls, Origin));
	}
}
