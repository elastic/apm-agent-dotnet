using System;
using System.Collections.Concurrent;

namespace Elastic.Apm.Logging
{
	internal class ScopedLogger : IApmLogger
	{
		public ScopedLogger(IApmLogger logger, string scope) => (Logger, Scope) = (logger, scope);

		private ConcurrentDictionary<string, LogValuesFormatter> Formatters { get; } = new ConcurrentDictionary<string, LogValuesFormatter>();

		public IApmLogger Logger { get; }

		public string Scope { get; }

		public LogValuesFormatter GetOrAddFormatter(string message, int expectedCount)
		{
			var formatter = Formatters.GetOrAdd(message, s => new LogValuesFormatter($"{{{{{{Scope}}}}}} {s}", Scope));
			if (formatter.ValueNames.Count != expectedCount)
			{
				//TODO log invalid structured logging? Should our LogValuesFormatter recover from this?
			}
			return formatter;
		}

		void IApmLogger.Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) =>
			Logger.Log(level, state, e, formatter);
	}
}
