using System;
using System.Collections.Generic;
using Elastic.Agent.Core.Logging;

namespace Elastic.Agent.Core.Config
{
    public class EnvironmentVariableConfig : IConfig
    {
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
                            Logger?.LogError($"Failed parsing Server URL from environment variable: {EnvVarConsts.ServerUrls}, value: {url}");
                            Logger?.LogDebug($"{e.GetType().Name}: {e.Message}");
                        }
                    }

                    if (serverUrls.Count == 0)
                    {
                        AddDefaultWithDebug();
                    }
                    return serverUrls;
                }
                else
                {
                    return serverUrls;
                }

                void AddDefaultWithDebug()
                {
                    serverUrls.Add(ConfigConsts.DefaultServerUri);
                    Logger?.LogDebug($"Using default ServerUrl: {ConfigConsts.DefaultServerUri}");
                }
            }
        }

        public AbstractLogger Logger { get; set; }
    }

    public static class EnvVarConsts
    {
        public static String ServerUrls => "ELASTIC_APM_SERVER_URLS";       
    }
}
