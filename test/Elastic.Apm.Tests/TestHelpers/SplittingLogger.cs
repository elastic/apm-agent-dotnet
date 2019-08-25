using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class SplittingLogger : IApmLogger
	{
		private readonly LogLevel _level;
		private readonly IApmLogger[] _targetLoggers;

		public SplittingLogger(params IApmLogger[] targetLoggers)
			: this(LogLevel.Trace, targetLoggers) { }

		public SplittingLogger(LogLevel level, params IApmLogger[] targetLoggers)
		{
			_level = level;
			_targetLoggers = targetLoggers;
		}

		public bool IsEnabled(LogLevel level) => _level <= level;

		public void Log<TState>(LogLevel level, TState state, Exception ex, Func<TState, Exception, string> formatter)
		{
			foreach (var targetLogger in _targetLoggers)
				targetLogger.Log<TState>(level, state, ex, formatter);
		}
	}
}
