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

		public IApmLoggerContext Context { get; } = new ApmLoggerContext();

		public LogLevel Level { get; set; }

		public bool IsEnabled(LogLevel level) => Level <= level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			var dateTime = DateTime.Now;

			var message = formatter(state, e);

			var fullMessage = $"[{dateTime:yyyy-MM-dd HH:mm:ss.fff zzz}][{ConsoleLogger.LevelToString(level)}] - {message}";
			if (e != null)
				fullMessage += $"{Environment.NewLine}+-> Exception: {e.GetType().FullName}: {e.Message}{Environment.NewLine}{e.StackTrace}";

			if (IsEnabled(level))
				_lineWriter.WriteLine(fullMessage);
		}
	}
}
