// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Threading;
using static Elastic.Apm.Config.ConfigConsts;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : IApmLogger, ILogLevelSwitchable
	{
		private static readonly object SyncRoot = new object();

		private readonly TextWriter _errorOut;
		private readonly TextWriter _standardOut;

		public ConsoleLogger(LogLevel level, TextWriter standardOut = null, TextWriter errorOut = null)
		{
			LogLevelSwitch = new LogLevelSwitch(level);
			_standardOut = standardOut ?? Console.Out;
			_errorOut = errorOut ?? Console.Error;
		}

		public static ConsoleLogger Instance { get; } = new ConsoleLogger(DefaultValues.LogLevel);

		public LogLevelSwitch LogLevelSwitch { get; }

		private LogLevel Level => LogLevelSwitch.Level;

		public static ConsoleLogger LoggerOrDefault(LogLevel? level)
		{
			if (level.HasValue && level.Value != DefaultValues.LogLevel)
				return new ConsoleLogger(level.Value);

			return Instance;
		}

		public bool IsEnabled(LogLevel level) => Level <= level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(level)) return;

			TextWriter writer;
			switch (level)
			{
				case LogLevel.Critical when Level <= LogLevel.Critical:
				case LogLevel.Error when Level <= LogLevel.Error:
					writer = _errorOut;
					break;
				case LogLevel.Warning when Level <= LogLevel.Warning:
				case LogLevel.Debug when Level <= LogLevel.Debug:
				case LogLevel.Information when Level <= LogLevel.Information:
				case LogLevel.Trace when Level <= LogLevel.Trace:
					writer = _standardOut;
					break;
				// ReSharper disable once RedundantCaseLabel
				case LogLevel.None:
				default:
					return;
			}

			var dateTime = DateTime.Now;
			var message = formatter(state, e);

			lock (SyncRoot)
			{
				writer.Write('[');
				writer.Write(dateTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
				writer.Write("][");
				writer.Write(Thread.CurrentThread.ManagedThreadId);
				writer.Write("][");
				writer.Write(LevelToString(level));
				writer.Write("] - ");
				writer.WriteLine(message);
				if (e != null)
				{
					writer.Write("+-> Exception: ");
					writer.Write(e.GetType().FullName);
					writer.Write(": ");
					writer.WriteLine(e.Message);
					writer.WriteLine(e.StackTrace);
				}

				writer.Flush();
			}
		}

		internal static string LevelToString(LogLevel level)
		{
			switch (level)
			{
				case LogLevel.Error: return "Error";
				case LogLevel.Warning: return "Warning";
				case LogLevel.Information: return "Info";
				case LogLevel.Debug: return "Debug";
				case LogLevel.Trace: return "Trace";
				case LogLevel.Critical: return "Critical";
				// ReSharper disable once RedundantCaseLabel
				case LogLevel.None:
				default: return "None";
			}
		}
	}
}
