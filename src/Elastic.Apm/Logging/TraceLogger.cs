// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Logging
{
	/// <summary>
	/// A logging implementation that logs to a <see cref="TraceSource"/> with the source name <c>Elastic.Apm</c>
	/// </summary>
	internal class TraceLogger : IApmLogger, ILogLevelSwitchable
	{
		private const string SourceName = "Elastic.Apm";

		private static readonly TraceSource TraceSource;

		static TraceLogger() => TraceSource = new TraceSource(SourceName);

		public TraceLogger(LogLevel level) => LogLevelSwitch = new LogLevelSwitch(level);

		public LogLevelSwitch LogLevelSwitch { get; }

		private LogLevel Level => LogLevelSwitch.Level;

		public bool IsEnabled(LogLevel level) => Level <= level;

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			if (!IsEnabled(level)) return;

			var message = formatter(state, e);

			// default message size is 51 + length of loglevel (max 8), message and exception.
			var builder = StringBuilderCache.Acquire(80);
			builder.Append('[');
			builder.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
			builder.Append("][");
			builder.Append(LevelToString(level));
			builder.Append("] - ");
			builder.Append(message);
			if (e != null)
			{
				builder.Append("+-> Exception: ");
				builder.Append(e.GetType().FullName);
				builder.Append(": ");
				builder.AppendLine(e.Message);
				builder.AppendLine(e.StackTrace);
			}

			var logMessage = StringBuilderCache.GetStringAndRelease(builder);
			for (var i = 0; i < TraceSource.Listeners.Count; i++)
			{
				var listener = TraceSource.Listeners[i];
				if (!listener.IsThreadSafe)
				{
					lock (listener)
						listener.WriteLine(logMessage);
				}
				else
					listener.WriteLine(logMessage);
			}

			TraceSource.Flush();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
