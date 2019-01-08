using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
    public abstract class AbstractAgentConfig
    {
        public AbstractLogger Logger { get; set; }

        /// <summary>
        /// Returns the value of the ServerUrls config
        /// </summary>
        /// <returns>
        /// value: the value of the setting,
        /// configType: the type of the config, eg. 'environment variable',
        /// configKey: the key (eg. 'ELASTIC_APM_SERVER_URLS') </returns>
        protected abstract (string value, string configType, string configKey) ReadServerUrls();
        protected abstract (string value, string configType, string configKey) ReadLogLevel();

        private List<Uri> _serverUrls = new List<Uri>();
        public List<Uri> ServerUrls
		{
			protected set => _serverUrls = value;
            get
            {
                if (_serverUrls.Count == 0)
                {
                    (string urlsStr, string configType, string configKey) = ReadServerUrls();

                    if (String.IsNullOrEmpty(urlsStr))
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

                    if (_serverUrls.Count == 0)
                    {
                        AddDefaultWithDebug();
                    }
                }

                return _serverUrls;

                void AddDefaultWithDebug()
                {
                    _serverUrls.Add(ConfigConsts.DefaultServerUri);
                    Logger?.LogDebug($"Using default ServerUrl: {ConfigConsts.DefaultServerUri}");
                }
            }
        }

        private LogLevel? _logLevel;
        public LogLevel LogLevel
        {
            get
            {
                if (!_logLevel.HasValue)
                {
                    (string logLevelStr,string configType, string configKey) = ReadLogLevel();

                    (var parsedLogLevel, var isError) = ParseLogLevel(logLevelStr);

                    if(isError)
                    {
                        _logLevel = LogLevel.Error;
                        Logger?.LogError($"Failed parsing log level from {configType}: {configKey}, value: {logLevelStr}. Defaulting to log level 'Error'");
                    }
                    else
                    {
                        _logLevel = parsedLogLevel.HasValue ? parsedLogLevel : LogLevel.Error;
                    }
                }

                return _logLevel.Value;
            }

            set => _logLevel = value;
        }

        protected (LogLevel? level, bool error) ParseLogLevel(string logLevelStr)
        {
            if (String.IsNullOrEmpty(logLevelStr))
            {
                return (null, false);
            }

            if (Enum.TryParse(logLevelStr, out LogLevel parsedLogLevel))
            {
                return (parsedLogLevel, false);
            }

            return (null, true);
        }
    }
}
