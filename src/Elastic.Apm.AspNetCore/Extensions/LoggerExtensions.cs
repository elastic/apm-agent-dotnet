using System;
using Microsoft.Extensions.Logging;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class LoggerExtensions
	{
		internal static LogLevel GetMinLogLevel(this ILogger logger)
		{
			if (logger == null) throw new ArgumentNullException(nameof(logger));

			if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Trace)) return LogLevel.Trace;

			if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug)) return LogLevel.Debug;

			if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information)) return LogLevel.Information;

			if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning)) return LogLevel.Warning;

			if (logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error)) return LogLevel.Error;

			return logger.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Critical) ? LogLevel.Critical : LogLevel.None;
		}
	}
}
