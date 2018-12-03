using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Agent.Core.Logging;

namespace Elastic.Agent.Core.Config
{
    internal class EnvironmentVariableConfig : IConfig
    {
        public AbstractLogger Logger { get; set; }

        private List<Uri> serverUrls = new List<Uri>();
        public List<Uri> ServerUrls { 
            get 
            {
                if (serverUrls.Count == 0)
                {
                    var urlsStr = Environment.GetEnvironmentVariable(EnvVarConsts.ServerUrls);

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
                            Logger?.LogError($"Failed parsing server URL from environment variable: {EnvVarConsts.ServerUrls}, value: {url}");
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

        private LogLevel? logLevel;
        public LogLevel LogLevel
        {
            get
            {
                if(!logLevel.HasValue)
                {
                    var logLevelStr = Environment.GetEnvironmentVariable(EnvVarConsts.LogLevel);

                    if (String.IsNullOrEmpty(logLevelStr))
                    {
                        logLevel = LogLevel.Error;
                        return logLevel.Value;
                    }
                  
                    if (Enum.TryParse(logLevelStr, out LogLevel parsedLogLevel))
                    {
                        logLevel = parsedLogLevel;
                    }
                    else
                    {
                        logLevel = LogLevel.Error;
                        Logger?.LogError($"Failed parsing log level from environment variable: {EnvVarConsts.LogLevel}, value: {logLevelStr}. Defaulting to log level 'Error'");
                    }
                }
               // Console.WriteLine("GetLogLevel");
                //Console.WriteLine(Environment.StackTrace.Substring(0,1580).ToString());
                return logLevel.Value;
            }

            set => logLevel = value;
        }
    }

    internal static class EnvVarConsts
    {
        public static String ServerUrls => "ELASTIC_APM_SERVER_URLS";
        public static String LogLevel => "ELASTIC_APM_LOG_LEVEL";
    }
}
