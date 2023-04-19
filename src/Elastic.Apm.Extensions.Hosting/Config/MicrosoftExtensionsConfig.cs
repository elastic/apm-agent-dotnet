﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Elastic.Apm.Extensions.Hosting.Config
{
	internal class MicrosoftExtensionsConfigReader : IConfigurationKeyValueProvider
	{
		internal const string Origin = "Microsoft.Extensions.Configuration";
		private readonly ScopedLogger _logger;
		private readonly IConfiguration _configuration;

		public MicrosoftExtensionsConfigReader(IConfiguration configuration, IApmLogger logger)
		{
			_logger = logger?.Scoped(nameof(MicrosoftExtensionsConfigReader));
			_configuration = configuration;
		}

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
	internal class MicrosoftExtensionsConfig : FallbackToEnvironmentConfigurationBase
	{
		private const string ThisClassName = nameof(MicrosoftExtensionsConfig);

		public MicrosoftExtensionsConfig(IConfiguration configuration, IApmLogger logger, string defaultEnvironmentName)
			: base(logger, defaultEnvironmentName, ThisClassName, new MicrosoftExtensionsConfigReader(configuration, logger)) =>
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
