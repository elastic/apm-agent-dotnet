using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Configuration;

namespace Elastic.Apm.AspNetCore.Config
{
	/// <summary>
	/// An agent-config provider based on Microsoft.Extensions.Configuration.IConfiguration.
	/// It uses environment variables as fallback
	/// </summary>
	public class MicrosoftExtensionsConfig : AbstractConfigurationReader, IConfigurationReader
	{
		private readonly IConfiguration _configuration;

		internal const string Origin = "Configuration Provider";

		public static (string LevelSubKey, string Level, string Urls, string ServiceName) Keys = (
			LevelSubKey: "LogLevel",
			Level: $"ElasticApm:LogLevel",
			Urls: "ElasticApm:ServerUrls",
			ServiceName: "ElasticApm:ServiceName"
		);

		public MicrosoftExtensionsConfig(IConfiguration configuration, AbstractLogger logger = null) : base(logger)
		{
			_configuration = configuration;
			_configuration.GetSection("ElasticApm")?
				.GetReloadToken()
				.RegisterChangeCallback(ChangeCallback, configuration.GetSection("ElasticApm"));
		}

		private LogLevel? _logLevel;
		public LogLevel LogLevel
		{
			get
			{
				if (_logLevel.HasValue) return _logLevel.Value;

				var l = ParseLogLevel(ReadFallBack(Keys.Level, ConfigConsts.ConfigKeys.Level));
				_logLevel = l;
				return l;
			}
		}

		public IReadOnlyList<Uri> ServerUrls => ParseServerUrls(ReadFallBack(Keys.Urls, ConfigConsts.ConfigKeys.Urls));

		public string ServiceName => ParseServiceName(ReadFallBack(Keys.ServiceName, ConfigConsts.ConfigKeys.ServiceName));

		private ConfigurationKeyValue Read(string key) => Kv(key, _configuration[key], Origin);

		private ConfigurationKeyValue ReadFallBack(string key, string fallBack)
		{
			var primary = Read(key);
			if (!string.IsNullOrWhiteSpace(primary.Value)) return primary;
			var secondary = Kv(key, _configuration[fallBack], EnvironmentConfigurationReader.Origin);
			return secondary;
		}

		private void ChangeCallback(object obj)
		{
			if (!(obj is IConfigurationSection section)) return;

			var newLogLevel = ParseLogLevel(Kv(Keys.LevelSubKey, section[Keys.LevelSubKey], Origin));
			if (_logLevel.HasValue && newLogLevel == _logLevel.Value) return;

			_logLevel = newLogLevel;
			Logger?.LogInfo($"Updated log level to {newLogLevel}");
		}
	}
}
