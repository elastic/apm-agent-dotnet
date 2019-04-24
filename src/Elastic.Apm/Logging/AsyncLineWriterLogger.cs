using System;

namespace Elastic.Apm.Logging
{
	internal class AsyncLineWriterLogger : IApmLogger
	{
		private readonly IAsyncLineWriter _errorOut;
		private readonly IAsyncLineWriter _standardOut;

		internal AsyncLineWriterLogger(LogLevel level, IAsyncLineWriter standardOut, IAsyncLineWriter errorOut = null)
		{
			Level = level;
			_standardOut = standardOut;
			_errorOut = errorOut ?? standardOut;
		}

		public LogLevel Level { get; }

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			var dateTime = DateTime.UtcNow;

			var message = formatter(state, e);

			var fullMessage = e == null ? $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{LevelToString(level)}] - {message}"
				: $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{LevelToString(level)}] - {message}{Environment.NewLine}Exception: {e.GetType().FullName}, Message: {e.Message}";

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

		private static string LevelToString(LogLevel level)
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
