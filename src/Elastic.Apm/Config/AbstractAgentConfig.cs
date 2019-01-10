using System;
using System.Collections.Generic;
using System.Reflection;
using Elastic.Apm.Logging;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

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

		protected AbstractAgentConfig(AbstractLogger logger, Service service, IPayloadSender payloadSender)
		{
			Logger = logger;
			Service = service;
			PayloadSender = payloadSender ?? new PayloadSender(logger, () => this.ServerUrls[0]);
		}

		public AbstractLogger Logger { get; }

		public IPayloadSender PayloadSender { get; }

		protected LogLevel? LogLevelBackingField;

		protected LogLevel? logLevel;
        public LogLevel LogLevel
        {
            get
            {
                if (!logLevel.HasValue)
                {
                    (var logLevelStr, var configType, var configKey) = ReadLogLevel();

                    (var parsedLogLevel, var isError) = ParseLogLevel(logLevelStr);

                    if(isError)
                    {
                        logLevel = LogLevel.Error;
                        Logger?.LogError("Config", $"Failed parsing log level from {configType}: {configKey}, value: {logLevelStr}. Defaulting to log level 'Error'");
                    }
                    else
                    {
                        logLevel = parsedLogLevel.HasValue ? parsedLogLevel : LogLevel.Error;
                    }
                }

                return logLevel.Value;
            }
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
						var name = GetType().Name;
						Logger?.LogError(name, $"Failed parsing server URL from {configType}: {configKey}, value: {url}");
						Logger?.LogDebug(name, $"{e.GetType().Name}: {e.Message}");
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

		/// <summary>
		/// Identifies the monitored service. If this remains unset the agent
		/// automatically populates it based on the entry assembly.
		/// </summary>
		/// <value>The service.</value>
		private Service _service;
		public Service Service
		{
			get => _service ?? (_service = new Service
			{
				Name = Assembly.GetEntryAssembly()?.GetName().Name,
				Agent = new Service.AgentC
				{
					Name = Consts.AgentName,
					Version = Consts.AgentVersion
				}
			});
			private set => _service = value;
		}

		protected static LogLevel LogLevelOrDefault(string logLevelStr)
		{
			if (string.IsNullOrEmpty(logLevelStr)) return AbstractLogger.LogLevelDefault;

			if (Enum.TryParse(logLevelStr, out LogLevel parsedLogLevel)) return parsedLogLevel;

			return AbstractLogger.LogLevelDefault;
		}

		protected static (LogLevel? level, bool error) ParseLogLevel(string logLevelStr)
		{
			if (string.IsNullOrEmpty(logLevelStr)) return (null, false);

			if (Enum.TryParse(logLevelStr, out LogLevel parsedLogLevel)) return (parsedLogLevel, false);

			return (null, true);
		}
	}
}
