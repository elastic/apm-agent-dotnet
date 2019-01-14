using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public class EnvironmentConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		internal const string Origin = "environment";

		internal static (string Level, string Urls) Keys = (
			Level: "ELASTIC_APM_LOG_LEVEL",
			Urls: "ELASTIC_APM_SERVER_URLS"
		);

		public EnvironmentConfigurationReader(AbstractLogger logger = null) : base(logger) { }

		public LogLevel LogLevel => ParseLogLevel(Read(Keys.Level));

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Read(Keys.Urls));

		private static ConfigurationKeyValue Read(string key) =>
			new ConfigurationKeyValue(key, Environment.GetEnvironmentVariable(key), Origin);
	}
}
