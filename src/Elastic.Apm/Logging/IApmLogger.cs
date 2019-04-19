using System;

namespace Elastic.Apm.Logging
{
	public interface IApmLogger
	{
		LogLevel Level { get; }

		void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter);
	}
}
