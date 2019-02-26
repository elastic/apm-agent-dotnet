using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestLogger : IApmLogger
	{
		public TestLogger() : this(LogLevel.Error) { }

		public TestLogger(LogLevel level) => LogLevel = level;

		public List<string> Lines { get; } = new List<string>();

		public LogLevel LogLevel { get; }

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			if (level >= LogLevel) Lines.Add(formatter(state, e));
		}
	}
}
