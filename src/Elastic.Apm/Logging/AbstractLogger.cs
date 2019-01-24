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
		/// Subclasses implement this method to write the logline to wherever
		/// they need to write it (e.g. into a file, to the console)
		/// </summary>
		/// <param name="logline">This line that must be logged - it already contains the prefix and the loglevel</param>
		protected abstract void PrintLogLine(string logLine);

		private string GetPrefixString(LogLevel logLevel, string prefix) =>
			string.IsNullOrWhiteSpace(prefix) ? $"{logLevel.ToString()} " : $"{logLevel.ToString()} {prefix}: ";

		protected AbstractLogger(LogLevel level) => LogLevel = level;

		public LogLevel LogLevel { get; }
		protected internal static LogLevel LogLevelDefault { get; } = LogLevel.Error;

		internal void LogInfo(string info) => LogInfo(null, info);
		internal void LogInfo(string prefix, string info)
		{
			if (LogLevel >= LogLevel.Info) PrintLogLine($"{GetPrefixString(LogLevel.Info, prefix)}{info}");
		}

		internal void LogWarning(string warning) => LogWarning(null, warning);
		internal void LogWarning(string prefix, string warning)
		{
			if (LogLevel >= LogLevel.Warning) PrintLogLine($"{GetPrefixString(LogLevel.Warning, prefix)}{warning}");
		}

		internal void LogError(string error) => LogError(null, error);
		internal void LogError(string prefix, string error)
		{
			if (LogLevel >= LogLevel.Error) PrintLogLine($"{GetPrefixString(LogLevel.Error, prefix)}{error}");
		}

		internal void LogDebug(string debug) => LogDebug(null, debug);
		internal void LogDebug(string prefix, string debug)
		{
			if (LogLevel >= LogLevel.Debug) PrintLogLine($"{GetPrefixString(LogLevel.Debug, prefix)}{debug}");
		}
	}
}
