using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class NoopLogger : IApmLogger
	{
		public LogLevel Level { get; set;  }

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) { }
	}
}
