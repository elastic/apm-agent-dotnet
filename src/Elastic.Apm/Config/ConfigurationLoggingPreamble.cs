// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using static Elastic.Apm.Config.ConfigConsts;
using static Elastic.Apm.Config.ConfigurationOption;

namespace Elastic.Apm.Config
{
	internal class ConfigurationOptionLogger
	{
		internal ConfigurationOptionLogger(ConfigurationOption option)
		{
			Option = option;
			EnvironmentVariableName = option.ToEnvironmentVariable();
			ConfigurationKeyName = option.ToConfigKey();
			NormalizedName = option.ToNormalizedName();
			NeedsMasking = Option switch
			{
				ApiKey => true,
				SecretToken => true,
				_ => false
			};
			LogAlways = Option switch
			{
				ServerUrl => true,
				ServiceName => true,
				ServiceVersion => true,
				LogLevel => true,
				_ => false
			};
		}

		public override string ToString() => $"{Option.ToNamedString()}";

		internal ConfigurationOption Option { get; }
		internal string ConfigurationKeyName { get; }
		internal string EnvironmentVariableName { get; }
		internal string NormalizedName { get; }
		internal bool NeedsMasking { get; }
		internal bool LogAlways { get; }

		public bool IsEssentialForLogging =>
			LogAlways || Option is SecretToken or ApiKey;
	}

	internal static class ConfigurationLoggingPreamble
	{
		internal static ApplicationKeyValue GetDefaultValueForLogging(ConfigurationOption option, IConfigurationReader config, string origin) =>
			option switch
			{
				ServerUrl => new (option, config.ServerUrl.AbsoluteUri, origin),
				ServiceName => new (option, config.ServiceName, origin),
				ServiceVersion => new (option, config.ServiceVersion, origin),
				LogLevel => new (option, config.LogLevel.ToString(), origin),
				SecretToken => new (option, config.SecretToken, origin),
				ApiKey => new (option, config.ApiKey, origin),
				_ => null,
			};

		internal static IReadOnlyCollection<ConfigurationOptionLogger> ConfigurationItems { get; } =
			ConfigurationOptionExtensions.AllOptions()
				.Select(o => new ConfigurationOptionLogger(o))
				.ToArray();
	}
}
