using System;

using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using System.Runtime.CompilerServices;
using Elastic.Apm.Api;
using Elastic.Apm.Report;

//TODO: It'd be nice to move this into the .csproj
[assembly: InternalsVisibleTo("Elastic.Apm.AspNetCore")]
[assembly: InternalsVisibleTo("Elastic.Apm.EntityFrameworkCore")]
[assembly: InternalsVisibleTo("Elastic.Apm.Tests")]
[assembly: InternalsVisibleTo("Elastic.Apm.AspNetCore.Tests")]

namespace Elastic.Apm
{
    public static class Agent
    {
        /// <summary>
        /// By default the agent reads configs from environment variables and it uses the <see cref="EnvironmentVariableConfig"/> class.
        /// This behaviour can be overwritten via the <see cref="Config"/> property. 
        /// For example in ASP.NET Core a <see cref="Elastic.Apm.AspNetCore.Config.MicrosoftExtensionsConfig"/> 
        /// with the actual <see cref="IConfiguration"/> instance can be created and passed to the agent. 
        /// With that the agent will read configs from the <see cref="IConfiguration"/> instance.
        /// </summary>
        private static AbstractAgentConfig config = new EnvironmentVariableConfig();

        /// <summary>
        /// The current agent config. This property stores all configs.
        /// </summary>
        /// <value>The config.</value>
        public static AbstractAgentConfig Config
        {
            get
            {
                if (config?.Logger == null)
                {
                    config.Logger = CreateLogger("Config");
                }

                return config;
            }
            set
            {
                config = value;
                config.Logger = CreateLogger("Config");
            }
        }

        private static IPayloadSender payloadSender;
        public static IPayloadSender PayloadSender
        {
            get
            {
                if (payloadSender == null)
                {
                    payloadSender = new PayloadSender();
                }

                return payloadSender;
            }

            internal set 
            {
                payloadSender = value;
            }
        }


        /// <summary>
        /// Returns a logger with a specific prefix. The idea behind this class 
        /// is that each component in the agent creates its own agent with a
        /// specific <paramref name="prefix"/> which makes correlating logs easier
        /// </summary>
        /// <returns>The logger.</returns>
        /// <param name="prefix">Prefix.</param>
        public static AbstractLogger CreateLogger(String prefix = "")
        {
            var logger = Activator.CreateInstance(loggerType) as AbstractLogger;
            logger.Prefix = prefix;
            return logger;          
        }

        /// <summary>
        /// Sets the type of the logger.
        /// </summary>
        /// <param name="t">T.</param>
        /// <typeparam name="T">The 1st type parameter.</typeparam>
        internal static void SetLoggerType<T>() where T : AbstractLogger, new()
        {
            loggerType = typeof(T);
        }

        private static ElasticApm api;

        public static IElasticApm Api => api ?? (api = new ElasticApm());

        static Type loggerType = typeof(ConsoleLogger);
    }
}
