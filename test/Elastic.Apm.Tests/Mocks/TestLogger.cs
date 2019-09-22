using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestLogger : IApmLogger
	{
		private readonly IApmLogger _baseLogger;
		private readonly ConsoleLogger _consoleLogger;
		private readonly StringWriter _writer;

		internal TestLogger() : this(ConsoleLogger.DefaultLogLevel, null) { }

		internal TestLogger(LogLevel level) : this(level, null) { }

		internal TestLogger(IApmLogger baseLogger) : this(ConsoleLogger.DefaultLogLevel, baseLogger) { }

		internal TestLogger(LogLevel level, IApmLogger baseLogger)
		{
			_baseLogger = baseLogger;
			_writer = new StringWriter();
			_consoleLogger = new ConsoleLogger(level, _writer, _writer);
		}

		public IApmLoggerContext Context => _baseLogger?.Context ?? _consoleLogger.Context;

		public bool IsEnabled(LogLevel level) => _consoleLogger.IsEnabled(level);

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			_baseLogger?.Log(level, state, e, formatter);
			_consoleLogger.Log(level, state, e, formatter);
		}

		internal IReadOnlyList<string> Lines =>
			_writer.GetStringBuilder().ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();
	}
}
