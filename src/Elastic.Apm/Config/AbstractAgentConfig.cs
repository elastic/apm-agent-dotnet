using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	public abstract class AbstractAgentConfig
	{
		protected abstract (string value, string configType, string configKey) ReadLogLevel();

		/// <summary>
		/// Returns the value of the ServerUrls config
		/// </summary>
		/// <returns>
		/// value: the value of the setting,
		/// configType: the type of the config, eg. 'environment variable',
		/// configKey: the key (eg. 'ELASTIC_APM_SERVER_URLS')
		/// </returns>
		protected abstract (string value, string configType, string configKey) ReadServerUrls();

		public AbstractLogger Logger { get; set; }

		protected LogLevel? LogLevelBackingField;

		public LogLevel LogLevel
		{
			get
			{
				if (LogLevelBackingField.HasValue) return LogLevelBackingField.Value;

				var (logLevelStr, configType, configKey) = ReadLogLevel();

				var (parsedLogLevel, isError) = ParseLogLevel(logLevelStr);

				if (isError)
				{
					LogLevelBackingField = LogLevel.Error;
					Logger?.LogError(
						$"Failed parsing log level from {configType}: {configKey}, value: {logLevelStr}. Defaulting to log level 'Error'");
				}
				else
					LogLevelBackingField = parsedLogLevel ?? LogLevel.Error;

				return LogLevelBackingField.Value;
			}

			set => LogLevelBackingField = value;
		}

		private readonly List<Uri> _serverUrls = new List<Uri>();

		public List<Uri> ServerUrls
		{
			get
			{
				if (_serverUrls.Count != 0) return _serverUrls;

				var (urlsStr, configType, configKey) = ReadServerUrls();

				if (string.IsNullOrEmpty(urlsStr))
				{
					AddDefaultWithDebug();
					return _serverUrls;
				}

				var urls = urlsStr?.Split(',');

				foreach (var url in urls)
				{
					try
					{
						_serverUrls.Add(new Uri(url));
					}
					catch (Exception e)
					{
						Logger?.LogError($"Failed parsing server URL from {configType}: {configKey}, value: {url}");
						Logger?.LogDebug($"{e.GetType().Name}: {e.Message}");
					}
				}

				if (_serverUrls.Count == 0) AddDefaultWithDebug();

				return _serverUrls;

				void AddDefaultWithDebug()
				{
					_serverUrls.Add(ConfigConsts.DefaultServerUri);
					Logger?.LogDebug($"Using default ServerUrl: {ConfigConsts.DefaultServerUri}");
				}
			}
		}

		protected (LogLevel? level, bool error) ParseLogLevel(string logLevelStr)
		{
			if (string.IsNullOrEmpty(logLevelStr)) return (null, false);

			if (Enum.TryParse(logLevelStr, out LogLevel parsedLogLevel)) return (parsedLogLevel, false);

			return (null, true);
		}
	}
}
