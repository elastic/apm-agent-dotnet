using System;

namespace Elastic.Apm.Logging
{
	public interface IApmLogger
	{
		void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter);
	}
}
