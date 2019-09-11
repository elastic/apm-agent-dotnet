using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class LineWriterToLoggerAdaptor : IApmLogger
	{
		private readonly ILineWriter _lineWriter;

		public LineWriterToLoggerAdaptor(ILineWriter lineWriter, LogLevel level = LogLevel.Information)
		{
			Level = level;
			_lineWriter = lineWriter;
		}

		public LogLevel Level { get; }

		public bool IsEnabled(LogLevel level) => Level <= level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			var dateTime = DateTime.UtcNow;

			var message = formatter(state, e);

			var fullMessage = e == null
				? $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{ConsoleLogger.LevelToString(level)}] - {message}"
				: $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{ConsoleLogger.LevelToString(level)}] - {message}{Environment.NewLine}+-> Exception: {e}";

			if (IsEnabled(level))
				_lineWriter.WriteLine(fullMessage);
		}
	}
}
