// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Logging;

#nullable enable
internal static class LoggingExtensions
{
	// Using a ConcurrentDictionary rather than ConditionalWeakTable as we expect few distinct scopes
	// and we want to retain them for reuse across the application lifetime. We use the scope name and the
	// instance of the base logger for the key, for rare scenarios when different base loggers might be
	// used. In reality, this only seems to affect testing scenarios.
	private static readonly ConcurrentDictionary<(string, IApmLogger), ScopedLogger> ScopedLoggers = new();

	private static readonly ConditionalWeakTable<string, LogValuesFormatter> Formatters = new();

	/// <summary>
	/// Returns a ScopedLogger, potentially from the cache.
	/// </summary>
	/// <param name="logger">An existing <see cref="IApmLogger"/>.</param>
	/// <param name="scope">The name of the scope.</param>
	/// <returns>A new scoped logger or <c>null</c> of the <see cref="IApmLogger"/> is <c>null</c>.</returns>
	/// <exception cref="ArgumentException">Requires <paramref name="scope"/> to be non-null and non-empty.</exception>
	internal static ScopedLogger? Scoped(this IApmLogger? logger, string scope)
	{
		if (string.IsNullOrEmpty(scope))
			throw new ArgumentException("Scope is required to be non-null and non-empty.", nameof(scope));

		if (logger is null)
			return null;

		var baseLogger = logger is ScopedLogger s ? s.Logger : logger;

		if (!ScopedLoggers.TryGetValue((scope, baseLogger), out var scopedLogger))
		{
			// Ensure we don't allow creations of scoped loggers 'wrapping' other scoped loggers
			var potentialScopedLogger = new ScopedLogger(baseLogger, scope);

			if (ScopedLoggers.TryAdd((scope, baseLogger), potentialScopedLogger))
			{
				scopedLogger = potentialScopedLogger;
			}
			else
			{
				scopedLogger = ScopedLoggers[(scope, baseLogger)];
			}
		}

		return scopedLogger;
	}

	private static void DoLog(this IApmLogger logger, LogLevel level, string message, Exception? exception, params object?[]? args)
	{
		try
		{
			var formatter = logger is ScopedLogger sl
				? sl.GetOrAddFormatter(message, args)
				: GetOrAddFormatter(message, args);

			var logValues = formatter.GetState(args);

			logger?.Log(level, logValues, exception, static (l, _) => l.Formatter.Format(l.Args));
		}
		catch (Exception ex)
		{
			// For now we will just print it to System.Diagnostics.Trace
			// In the future we should consider error counters to increment and log periodically on a worker thread
			try
			{
				var newLine = Environment.NewLine + "Elastic APM .NET Agent: ";
				var currentStackTraceFrames = new StackTrace(true).GetFrames();
				var currentStackTrace = currentStackTraceFrames == null
					? " N/A"
					: newLine + string.Join("", currentStackTraceFrames.Select(f => "    " + f));

				System.Diagnostics.Trace.WriteLine("Elastic APM .NET Agent: [CRITICAL] Exception thrown by logging implementation."
					+ $" Log message: `{message.AsNullableToString()}'."
					+ $" args.Length: {args?.Length ?? 0}."
					+ $" Current thread: {DbgUtils.CurrentThreadDesc}"
					+ newLine
					+ $"+-> Exception (exception): {ex.GetType().FullName}: {ex.Message}{newLine}{ex.StackTrace}"
					+ (exception != null
						? newLine + $"+-> Exception (e): {exception.GetType().FullName}: {exception.Message}{newLine}{exception.StackTrace}"
						: $"e: {ObjectExtensions.NullAsString}")
					+ newLine
					+ "+-> Current stack trace:" + currentStackTrace
				);
			}
			catch (Exception)
			{
				// ignored
			}
		}
	}

#if !NET8_0_OR_GREATER
	private static readonly object _lock = new();
#endif

	private static LogValuesFormatter GetOrAddFormatter(string message, IReadOnlyCollection<object?>? args)
	{
		if (Formatters.TryGetValue(message, out var formatter))
			return formatter;

		formatter = new LogValuesFormatter(message, args);
#if NET8_0_OR_GREATER
		Formatters.AddOrUpdate(message, formatter);
		return formatter;
#else
		lock (_lock)
		{
			if (Formatters.TryGetValue(message, out var f))
				return f;
			Formatters.Add(message, formatter);
			return formatter;
		}
#endif
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

		public void Log(string message, params object?[]? args) => _logger.DoLog(_level, message, null, args);

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
#nullable restore
