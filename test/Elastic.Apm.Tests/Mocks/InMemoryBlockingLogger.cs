using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	/// <summary>
	/// An in-memory logger which blocks until the 1. log line is received or a timeout is reached
	/// </summary>
	public class InMemoryBlockingLogger : IApmLogger
	{
		private readonly List<string> _lines = new List<string>();
		private readonly LogLevel _logLevel;

		private readonly TaskCompletionSource<List<string>> _transactionTaskCompletionSource = new TaskCompletionSource<List<string>>();

		public InMemoryBlockingLogger(LogLevel level) => _logLevel = level;

		/// <summary>
		/// Returns the log lines that the logger collected.
		/// Blocks until as long as the list is empty or a timeout is reached.
		/// </summary>
		public IEnumerable<string> Lines
		{
			get
			{
				var timer = new Timer { Interval = 10000, Enabled = true };
				timer.Elapsed += (a, b) =>
				{
					_transactionTaskCompletionSource.TrySetCanceled();
					timer.Stop();
				};
				timer.Start();

				try
				{
					_transactionTaskCompletionSource.Task.Wait();
					return _transactionTaskCompletionSource.Task.Result;
				}
				catch
				{
					return new List<string>();
				}
			}
		}

		public bool IsEnabled(LogLevel level)
			=> _logLevel <= level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(level)) return;

			_lines.Add(formatter(state, e));
			_transactionTaskCompletionSource.TrySetResult(_lines);
		}
	}
}
