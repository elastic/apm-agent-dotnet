using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	internal class NoopLogger : IApmLogger
	{
		public LogLevel Level => LogLevel.None;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) { }
	}
}
