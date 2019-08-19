using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	internal class NoopLogger : IApmLogger
	{
		public bool IsEnabled(LogLevel level) => false;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) { }
	}
}
