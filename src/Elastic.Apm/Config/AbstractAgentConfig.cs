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

        protected List<Uri> serverUrls = new List<Uri>();
        public List<Uri> ServerUrls
        {
            get
            {
                if (serverUrls.Count == 0)
                {
                    (string urlsStr, string configType, string configKey) = ReadServerUrls();

                    if (String.IsNullOrEmpty(urlsStr))
                    {
                        AddDefaultWithDebug();
                        return serverUrls;
                    }

                    var urls = urlsStr?.Split(',');

                    foreach (var url in urls)
                    {
                        try
                        {
                            serverUrls.Add(new Uri(url));
                        }
                        catch (Exception e)
                        {
                            Logger?.LogError($"Failed parsing server URL from {configType}: {configKey}, value: {url}");
                            Logger?.LogDebug($"{e.GetType().Name}: {e.Message}");
                        }
                    }

                    if (serverUrls.Count == 0)
                    {
                        AddDefaultWithDebug();
                    }
                }

                return serverUrls;

                void AddDefaultWithDebug()
                {
                    serverUrls.Add(ConfigConsts.DefaultServerUri);
                    Logger?.LogDebug($"Using default ServerUrl: {ConfigConsts.DefaultServerUri}");
                }
            }
        }

        protected LogLevel? logLevel;
        public LogLevel LogLevel
        {
            get
            {
                if (!logLevel.HasValue)
                {
                    (string logLevelStr,string configType, string configKey) = ReadLogLevel();

                    (var parsedLogLevel, var isError) = ParseLogLevel(logLevelStr);

                    if(isError)
                    {
                        logLevel = LogLevel.Error;
                        Logger?.LogError($"Failed parsing log level from {configType}: {configKey}, value: {logLevelStr}. Defaulting to log level 'Error'");
                    }
                    else
                    {
                        logLevel = parsedLogLevel.HasValue ? parsedLogLevel : LogLevel.Error;
                    }
                }

                return logLevel.Value;
            }

            set => logLevel = value;
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
