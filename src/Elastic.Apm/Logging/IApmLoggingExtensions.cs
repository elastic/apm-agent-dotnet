using System;
using System.Collections.Concurrent;
using System.IO;

namespace Elastic.Apm.Logging
{
	internal static class LoggingExtensions
	{
		private static ConcurrentDictionary<string, LogValuesFormatter> Formatters { get; } = new ConcurrentDictionary<string, LogValuesFormatter>();

		public static void LogError(this IApmLogger logger, string message, params object[] args) => logger?.LogError(null, message, args);

		public static void LogError(this IApmLogger logger, Exception exception, string message, params object[] args) =>
			logger?.DoLog(LogLevel.Error, message, exception, args);

		public static void LogWarning(this IApmLogger logger, string message, params object[] args) => logger?.LogWarning(null, message, args);

		public static void LogWarning(this IApmLogger logger, Exception exception, string message, params object[] args) =>
			logger?.DoLog(LogLevel.Warning, message, exception, args);

		public static void LogInfo(this IApmLogger logger, string message, params object[] args) => logger?.LogInfo(null, message, args);

		public static void LogInfo(this IApmLogger logger, Exception exception, string message, params object[] args) =>
			logger?.DoLog(LogLevel.Information, message, exception, args);

		public static void LogDebug(this IApmLogger logger, string message, params object[] args) => logger?.LogDebug(null, message, args);

		public static void LogDebug(this IApmLogger logger, Exception exception, string message, params object[] args) =>
			logger?.DoLog(LogLevel.Debug, message, exception, args);

		public static void LogDebugException(this IApmLogger logger, Exception exception) =>
			logger?.LogDebug(exception, "{ExceptionName}: {ExceptionMessage}", exception?.GetType().Name, exception?.Message);

		public static void LogErrorException(this IApmLogger logger, Exception exception,
			[System.Runtime.CompilerServices.CallerMemberName]
			string method = "",
			[System.Runtime.CompilerServices.CallerFilePath]
			string filepath = "",
			[System.Runtime.CompilerServices.CallerLineNumber]
			int lineNumber = 0
		)
		{
			var file = string.IsNullOrEmpty(filepath) ? string.Empty : new FileInfo(filepath).Name;
			logger?.LogError(exception, "{ExceptionName} in {Method} ({File}:{LineNumber}): {ExceptionMessage}", exception?.GetType().Name, method, file, lineNumber, exception?.Message);
		}

		public static ScopedLogger Scoped(this IApmLogger logger, string scope) =>
			 new ScopedLogger((logger is ScopedLogger s ? s.Logger : logger), scope);

		private static void DoLog(this IApmLogger logger, LogLevel level, string message, Exception e, object[] args)
		{
			var formatter = logger is ScopedLogger sl
				? sl.GetOrAddFormatter(message, args.Length)
				: Formatters.GetOrAdd(message, s => new LogValuesFormatter(s));

			var logValues = formatter.GetState(args);
			logger?.Log(level, logValues, e, (s, _) => formatter.Format(args));
		}
	}
}
