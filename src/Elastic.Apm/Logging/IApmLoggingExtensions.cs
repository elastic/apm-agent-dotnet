using System;
using System.Collections.Concurrent;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Logging
{
	internal static class LoggingExtensions
	{
		private static ConcurrentDictionary<string, LogValuesFormatter> Formatters { get; } = new ConcurrentDictionary<string, LogValuesFormatter>();

		internal static ScopedLogger Scoped(this IApmLogger logger, string scope) =>
			new ScopedLogger(logger is ScopedLogger s ? s.Logger : logger, scope);

		private static void DoLog(this IApmLogger logger, LogLevel level, string message, Exception e, params object[] args)
		{
			try
			{
				var formatter = logger is ScopedLogger sl
					? sl.GetOrAddFormatter(message, args)
					: Formatters.GetOrAdd(message, s => new LogValuesFormatter(s, args));

				var logValues = formatter.GetState(args);

				logger?.Log(level, logValues, e, (s, _) => formatter.Format(args));
			}
			catch (Exception exception)
			{
				// For now we will just print it to System.Diagnostics.Trace
				// In the future we should consider error counters to increment and log periodically on a worker thread
				try
				{
					var currentStackTraceFrames = new System.Diagnostics.StackTrace(true).GetFrames();
					var currentStackTrace = currentStackTraceFrames == null
						? " N/A"
						: Environment.NewLine + string.Join("", currentStackTraceFrames.Select(f => "    " + f));

					System.Diagnostics.Trace.WriteLine("Elastic APM .NET Agent: [CRITICAL] Exception thrown by logging implementation."
						+ $" Log message: `{message.AsNullableToString()}'."
						+ $" args.Length: {args.Length}."
						+ $" Current thread: name: `{Thread.CurrentThread.Name.AsNullableToString()}',"
						+ $" managed ID: {Thread.CurrentThread.ManagedThreadId}."
						+ Environment.NewLine
						+ $"+-> Exception (exception): {exception.GetType().FullName}: {exception.Message}{Environment.NewLine}{exception.StackTrace}"
						+ (e != null
							? Environment.NewLine + $"+-> Exception (e): {e.GetType().FullName}: {e.Message}{Environment.NewLine}{e.StackTrace}"
							: $"e: {ObjectExtensions.NullAsString}")
						+ Environment.NewLine
						+ "+-> Current stack trace:" + currentStackTrace
					);
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}

		/// <summary>
		/// Depending on the two parameters it either returns a MaybeLogger instance or null.
		/// </summary>
		/// <param name="logger">The logger you want to log with</param>
		/// <param name="level">The level to compare with</param>
		/// <returns>If the return value is not null you can call <see cref="MaybeLogger.Log" /> to log</returns>
		internal static MaybeLogger? IfLevel(this IApmLogger logger, LogLevel level) =>
			logger.IsEnabled(level) ? new MaybeLogger(logger, level) : (MaybeLogger?)null;

		/// <summary>
		/// If the logger has a loglevel, which is higher than or equal to Trace then it returns a MaybeLogger instance,
		/// otherwise it returns null.
		/// By using the return value with `?.` you can avoid executing code that is not necessary to execute
		/// in case the log won't be printed because the loglevel would not allow it.
		/// </summary>
		/// <param name="logger">The logger instance you want to log with</param>
		/// <returns>Either a MaybeLogger or null</returns>
		internal static MaybeLogger? Trace(this IApmLogger logger) => IfLevel(logger, LogLevel.Trace);

		/// <summary>
		/// If the logger has a loglevel, which is higher than or equal to Debug then it returns a MaybeLogger instance,
		/// otherwise it returns null.
		/// By using the return value with `?.` you can avoid executing code that is not necessary to execute
		/// in case the log won't be printed because the loglevel would not allow it.
		/// </summary>
		/// <param name="logger">The logger instance you want to log with</param>
		/// <returns>Either a MaybeLogger or null</returns>
		internal static MaybeLogger? Debug(this IApmLogger logger) => IfLevel(logger, LogLevel.Debug);

		/// <summary>
		/// If the logger has a loglevel, which is higher than Info then it returns a MaybeLogger instance,
		/// otherwise it returns null.
		/// By using the return value with `?.` you can avoid executing code that is not necessary to execute
		/// in case the log won't be printed because the loglevel would not allow it.
		/// </summary>
		/// <param name="logger">The logger instance you want to log with</param>
		/// <returns>Either a MaybeLogger or null</returns>
		internal static MaybeLogger? Info(this IApmLogger logger) => IfLevel(logger, LogLevel.Information);

		/// <summary>
		/// If the logger has a loglevel, which is higher than or equal to Warning then it returns a MaybeLogger instance,
		/// otherwise it returns null.
		/// By using the return value with `?.` you can avoid executing code that is not necessary to execute
		/// in case the log won't be printed because the loglevel would not allow it.
		/// </summary>
		/// <param name="logger">The logger instance you want to log with</param>
		/// <returns>Either a MaybeLogger or null</returns>
		internal static MaybeLogger? Warning(this IApmLogger logger) => IfLevel(logger, LogLevel.Warning);

		/// <summary>
		/// If the logger has a loglevel, which is higher than or equal to Error then it returns a MaybeLogger instance,
		/// otherwise it returns null.
		/// By using the return value with `?.` you can avoid executing code that is not necessary to execute
		/// in case the log won't be printed because the loglevel would not allow it.
		/// </summary>
		/// <param name="logger">The logger instance you want to log with</param>
		/// <returns>Either a MaybeLogger or null</returns>
		internal static MaybeLogger? Error(this IApmLogger logger) => IfLevel(logger, LogLevel.Error);

		/// <summary>
		/// If the logger has a loglevel, which is higher than or equal to Critical then it returns a MaybeLogger instance,
		/// otherwise it returns null.
		/// By using the return value with `?.` you can avoid executing code that is not necessary to execute
		/// in case the log won't be printed because the loglevel would not allow it.
		/// </summary>
		/// <param name="logger">The logger instance you want to log with</param>
		/// <returns>Either a MaybeLogger or null</returns>
		internal static MaybeLogger? Critical(this IApmLogger logger) => IfLevel(logger, LogLevel.Critical);

		internal readonly struct MaybeLogger
		{
			private readonly IApmLogger _logger;
			private readonly LogLevel _level;

			public MaybeLogger(IApmLogger logger, LogLevel level) => (_logger, _level) = (logger, level);

			public void Log(string message, params object[] args) => _logger.DoLog(_level, message, null, args);

			public void LogException(Exception exception, string message, params object[] args) =>
				_logger.DoLog(_level, message, exception, args);

			public void LogExceptionWithCaller(Exception exception,
				[CallerMemberName] string method = "",
				[CallerFilePath] string filePath = "",
				[CallerLineNumber] int lineNumber = 0
			)
			{
				var file = string.IsNullOrEmpty(filePath) ? string.Empty : new FileInfo(filePath).Name;
				_logger?.DoLog(_level, "{ExceptionName} in {Method} ({File}:{LineNumber}): {ExceptionMessage}", exception,
					exception?.GetType().Name, method, file, lineNumber, exception?.Message);
			}
		}
	}
}
