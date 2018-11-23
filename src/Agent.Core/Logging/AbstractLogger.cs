using System;
namespace Elastic.Agent.Core.Logging
{
    /// <summary>
    /// Base class for every logger.
    /// It filters logs based on log levels and also concatenates strings
    /// that will be printed by subtypes of this type.
    /// </summary>
    public abstract class AbstractLogger
    {
        /// <summary>
        /// Every log message is prefixed with this string
        /// </summary>
        /// <value>The prefix.</value>
        internal String Prefix { get; set; }

        private String GetPrefixString(LogLevel logLevel) => $"{logLevel.ToString()} {Prefix}: ";

        public void LogInfo(String info)
        {
            if (Apm.Agent.LogLevel >= LogLevel.Info)
            {
                PrintLogline($"{GetPrefixString(LogLevel.Info)}{info}");
            }
        }

        public void LogWarning(String warning)
        {
            if (Apm.Agent.LogLevel >= LogLevel.Warning)
            {
                PrintLogline($"{GetPrefixString(LogLevel.Warning)}{warning}");
            }
        }

        public void LogError(String error)
        {
            if (Apm.Agent.LogLevel >= LogLevel.Error)
            {
                PrintLogline($"{GetPrefixString(LogLevel.Error)}{error}");
            }
        }

        public void LogDebug(String debugInfo)
        {
            if (Apm.Agent.LogLevel >= LogLevel.Debug)
            {
                PrintLogline($"{GetPrefixString(LogLevel.Debug)}{debugInfo}");
            }
        }

        /// <summary>
        /// Subclasses implement this method to write the logline to wherever 
        /// they need to write it (e.g. into a file, to the console)
        /// </summary>
        /// <param name="logline">This line that must be logged - it already contains the prefix and the loglevel</param>
        protected abstract void PrintLogline(String logline);
    }
}
