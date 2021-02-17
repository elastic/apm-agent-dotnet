using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Timer = System.Timers.Timer;

namespace Elastic.Apm.Tests.Utilities
{
	/// <summary>
	/// An in-memory logger which blocks until the 1. log line is received or a timeout is reached
	/// </summary>
	public class InMemoryBlockingLogger : IApmLogger
	{
		private readonly List<string> _lines = new List<string>();
		private readonly LogLevel _logLevel;
		private readonly ManualResetEvent _waitHandle;

		public InMemoryBlockingLogger(LogLevel level)
		{
			_logLevel = level;
			_waitHandle = new ManualResetEvent(false);
		}

		/// <summary>
		/// Returns the log lines that the logger collected.
		/// Blocks until as long as the list is empty or a timeout is reached.
		/// </summary>
		public IEnumerable<string> Lines
		{
			get
			{
				_waitHandle.WaitOne(TimeSpan.FromMinutes(1));
				return _lines;
			}
		}

		public bool IsEnabled(LogLevel level)
			=> _logLevel <= level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(level)) return;

			_lines.Add(formatter(state, e));
			_waitHandle.Set();
		}
	}
}
