using System;
using System.IO;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : IApmLogger
	{
		private readonly TextWriter _errorOut;
		private readonly LogLevel _level;
		private readonly TextWriter _standardOut;

		public ConsoleLogger(LogLevel level, TextWriter standardOut = null, TextWriter errorOut = null)
		{
			_level = level;
			_standardOut = standardOut ?? Console.Out;
			_errorOut = standardOut ?? Console.Error;
		}

		protected internal static LogLevel DefaultLogLevel { get; } = LogLevel.Error;
		public static ConsoleLogger Instance { get; } = new ConsoleLogger(DefaultLogLevel);

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			//TODO: Discuss UtcNow?
			var dateTime = DateTime.UtcNow;

			var message = formatter(state, e);
			var fullMessage = $"[{dateTime.ToString("yyyy-M-d hh:mm:ss")}][{Level(level)}] - {message}";
			switch (level)
			{
				case LogLevel.Critical when _level >= LogLevel.Critical:
				case LogLevel.Error when _level >= LogLevel.Error:
					_errorOut.WriteLineAsync(fullMessage);
					break;
				case LogLevel.Warning when _level >= LogLevel.Warning:
				case LogLevel.Debug when _level >= LogLevel.Debug:
				case LogLevel.Information when _level >= LogLevel.Information:
				case LogLevel.Trace when _level >= LogLevel.Trace:
					_standardOut.WriteLineAsync(fullMessage);
					break;
				case LogLevel.None: break;
				default: break;
			}
		}

		private static string Level(LogLevel level)
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
