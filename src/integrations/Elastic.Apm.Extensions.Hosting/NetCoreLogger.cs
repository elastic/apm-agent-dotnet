using System;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.Extensions.Hosting
{
	internal class NetCoreLogger : IApmLogger
	{
		private readonly ILogger _logger;

		public NetCoreLogger(ILoggerFactory loggerFactory) =>
			_logger = loggerFactory?.CreateLogger("Elastic.Apm") ?? throw new ArgumentNullException(nameof(loggerFactory));

		public bool IsEnabled(LogLevel level) => _logger.IsEnabled(Convert(level));

		public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) =>
			_logger.Log(Convert(level), new EventId(), state, e, formatter);

		private static Microsoft.Extensions.Logging.LogLevel Convert(LogLevel logLevel)
		{
			switch (logLevel)
			{
				case LogLevel.Trace: return Microsoft.Extensions.Logging.LogLevel.Trace;
				case LogLevel.Debug: return Microsoft.Extensions.Logging.LogLevel.Debug;
				case LogLevel.Information: return Microsoft.Extensions.Logging.LogLevel.Information;
				case LogLevel.Warning: return Microsoft.Extensions.Logging.LogLevel.Warning;
				case LogLevel.Error: return Microsoft.Extensions.Logging.LogLevel.Error;
				case LogLevel.Critical: return Microsoft.Extensions.Logging.LogLevel.Critical;
				// ReSharper disable once RedundantCaseLabel
				case LogLevel.None:
				default: return Microsoft.Extensions.Logging.LogLevel.None;
			}
		}
	}
}
