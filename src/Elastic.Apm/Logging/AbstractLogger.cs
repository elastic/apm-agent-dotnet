using System;

namespace Elastic.Apm.Logging
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
		internal string Prefix { get; set; }

		private string GetPrefixString(LogLevel logLevel) => $"{logLevel.ToString()} {Prefix}: ";

		public void LogInfo(string info)
		{
			if (Agent.Config.LogLevel >= LogLevel.Info) PrintLogline($"{GetPrefixString(LogLevel.Info)}{info}");
		}

		public void LogWarning(string warning)
		{
			if (Agent.Config.LogLevel >= LogLevel.Warning) PrintLogline($"{GetPrefixString(LogLevel.Warning)}{warning}");
		}

		public void LogError(string error)
		{
			if (Agent.Config.LogLevel >= LogLevel.Error) PrintLogline($"{GetPrefixString(LogLevel.Error)}{error}");
		}

		public void LogDebug(string debugInfo)
		{
			if (Agent.Config.LogLevel >= LogLevel.Debug) PrintLogline($"{GetPrefixString(LogLevel.Debug)}{debugInfo}");
		}

		/// <summary>
		/// Subclasses implement this method to write the logline to wherever
		/// they need to write it (e.g. into a file, to the console)
		/// </summary>
		/// <param name="logline">This line that must be logged - it already contains the prefix and the loglevel</param>
		protected abstract void PrintLogline(string logline);
	}
}
