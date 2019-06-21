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

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			var dateTime = DateTime.UtcNow;

			var message = formatter(state, e);

			var fullMessage = e == null
				? $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{ConsoleLogger.LevelToString(level)}] - {message}"
				: $"[{dateTime.ToString("yyyy-MM-dd hh:mm:ss")}][{ConsoleLogger.LevelToString(level)}] - {message}{Environment.NewLine}Exception: {e.GetType().FullName}, Message: {e.Message}";

			switch (level)
			{
				case LogLevel.Critical when Level <= LogLevel.Critical:
				case LogLevel.Error when Level <= LogLevel.Error:
				case LogLevel.Warning when Level <= LogLevel.Warning:
				case LogLevel.Debug when Level <= LogLevel.Debug:
				case LogLevel.Information when Level <= LogLevel.Information:
				case LogLevel.Trace when Level <= LogLevel.Trace:
					_lineWriter.WriteLine(fullMessage);
					break;
				case LogLevel.None: break;
			}
		}
	}
}
