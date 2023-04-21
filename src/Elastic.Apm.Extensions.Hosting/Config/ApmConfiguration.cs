// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Elastic.Apm.Extensions.Hosting.Config
{
	internal class ConfigurationKeyValueProvider : IConfigurationKeyValueProvider
	{
		internal const string Origin = "Microsoft.Extensions.Configuration";
		private readonly ScopedLogger _logger;
		private readonly IConfiguration _configuration;

		public ConfigurationKeyValueProvider(IConfiguration configuration, IApmLogger logger)
		{
			_logger = logger?.Scoped(nameof(ConfigurationKeyValueProvider));
			_configuration = configuration;
		}

		public string Description => Origin;

		public ConfigurationKeyValue Read(string key)
		{
			var value = _configuration[key];
			return value != null ? new ConfigurationKeyValue(key, value, Origin) : null;
		}
	}

	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback
	/// </summary>
	internal class ApmConfiguration : FallbackToEnvironmentConfigurationBase
	{
		private const string ThisClassName = nameof(ApmConfiguration);

		public ApmConfiguration(IConfiguration configuration, IApmLogger logger, string defaultEnvironmentName)
			: base(logger,
				new ConfigurationDefaults { EnvironmentName = defaultEnvironmentName, DebugName = ThisClassName },
				new ConfigurationKeyValueProvider(configuration, logger)) =>
			configuration.GetSection("ElasticApm")
				?
				.GetReloadToken()
				.RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));

		private void ChangeCallback(object obj)
		{
			if (obj is not IConfigurationSection) return;

			var newLogLevel = ParseLogLevel(Read(ConfigConsts.KeyNames.LogLevel, ConfigConsts.EnvVarNames.LogLevel));
			if (LogLevel == newLogLevel) return;
			LogLevel = newLogLevel;
		}
	}
}
