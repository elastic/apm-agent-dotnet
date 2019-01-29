using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class EnvironmentConfigurationReader : AbstractConfigurationReader, IConfigurationReader
	{
		internal const string Origin = "environment";

		public EnvironmentConfigurationReader(AbstractLogger logger) : base(logger) { }

		public LogLevel LogLevel => ParseLogLevel(Read(ConfigConsts.ConfigKeys.Level));

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(Read(ConfigConsts.ConfigKeys.Urls));

		public string ServiceName => ParseServiceName(Read(ConfigConsts.ConfigKeys.ServiceName));

		private static ConfigurationKeyValue Read(string key) =>
			new ConfigurationKeyValue(key, Environment.GetEnvironmentVariable(key), Origin);
	}
}
