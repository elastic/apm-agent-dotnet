using System;
using System.IO;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : IApmLogger
	{
		private readonly TextWriter _errorOut;
		private readonly TextWriter _standardOut;

		public ConsoleLogger(LogLevel level, TextWriter standardOut = null, TextWriter errorOut = null)
		{
			Level = level;
			_standardOut = standardOut ?? Console.Out;
			_errorOut = errorOut ?? Console.Error;
		}

		protected internal static LogLevel DefaultLogLevel { get; } = LogLevel.Error;
		public static ConsoleLogger Instance { get; } = new ConsoleLogger(DefaultLogLevel);

		public static ConsoleLogger LoggerOrDefault(LogLevel? level)
		{
			if (level.HasValue && level.Value != DefaultLogLevel)
				return new ConsoleLogger(level.Value);

			return Instance;
		}

		private LogLevel Level { get; }

		public bool IsEnabled(LogLevel level) => Level <= level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(level)) return;

			var dateTime = DateTime.UtcNow;
			var message = formatter(state, e);

			var fullMessage = e == null
				? $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{LevelToString(level)}] - {message}"
				: $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{LevelToString(level)}] - {message}{Environment.NewLine}+-> Exception: {e}";

			switch (level)
			{
				case LogLevel.Critical when Level <= LogLevel.Critical:
				case LogLevel.Error when Level <= LogLevel.Error:
					_errorOut.WriteLineAsync(fullMessage);
					break;
				case LogLevel.Warning when Level <= LogLevel.Warning:
				case LogLevel.Debug when Level <= LogLevel.Debug:
				case LogLevel.Information when Level <= LogLevel.Information:
				case LogLevel.Trace when Level <= LogLevel.Trace:
					_standardOut.WriteLineAsync(fullMessage);
					break;
				case LogLevel.None: break;
			}
		}

		internal static string LevelToString(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Error: return "Error";
				case LogLevel.Warning: return "Warning";
				case LogLevel.Information: return "Info";
				case LogLevel.Debug: return "Debug";
				case LogLevel.Trace: return "Trace";
				case LogLevel.Critical: return "Critical";
				case LogLevel.None:
				default: return "None";
			}
		}
	}
}
