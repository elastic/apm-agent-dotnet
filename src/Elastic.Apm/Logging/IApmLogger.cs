using System;
using System.Collections.Generic;

namespace Elastic.Apm.Logging
{
	public interface IApmLogger
	{
		IApmLoggerContext Context { get; }

		bool IsEnabled(LogLevel level);

		void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter);
	}

	public interface IApmLoggerContext
	{
		string this[string key] { get; set; }

		IReadOnlyDictionary<string, string> Copy();
	}

	public class ApmLoggerContext: IApmLoggerContext
	{
		private readonly object _lock = new object();
		private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>();

		public string this[string key]
		{
			get
			{
				lock (_lock) return _dictionary[key];
			}

			set
			{
				lock (_lock) _dictionary[key] = value;
			}
		}

		public IReadOnlyDictionary<string, string> Copy()
		{
			lock (_lock) return new Dictionary<string, string>(_dictionary);
		}
	}
}
