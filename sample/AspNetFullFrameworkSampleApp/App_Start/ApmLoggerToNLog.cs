using System;
using Elastic.Apm.Logging;
using NLog;
using NLogLevel = NLog.LogLevel;
using ApmLogLevel = Elastic.Apm.Logging.LogLevel;

namespace AspNetFullFrameworkSampleApp
{
	internal class ApmLoggerToNLog : IApmLogger
	{
		private readonly Lazy<Logger> _logger;

		public bool IsEnabled(ApmLogLevel level) => _logger.Value.IsEnabled(ConvertLevel(level));

		public ApmLoggerToNLog()
			:this(string.Empty)
		{

		}

		public ApmLoggerToNLog(string loggerName) => _logger = new Lazy<Logger>(() => LogManager.GetLogger(loggerName ?? string.Empty), true);

		public void Log<TState>(ApmLogLevel apmLogLevel, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			var nlogLevel = ConvertLevel(apmLogLevel);
			var nlogger = _logger.Value;
			if (!nlogger.IsEnabled(nlogLevel))
				return;

			var message = formatter(state, e);
			if (e == null)
				nlogger.Log(nlogLevel, message);
			else
				nlogger.Log(nlogLevel, e, message);
		}

		private static NLogLevel ConvertLevel(ApmLogLevel apmLogLevel)
		{
			switch (apmLogLevel)
			{
				case ApmLogLevel.Trace: return NLogLevel.Trace;
				case ApmLogLevel.Debug: return NLogLevel.Debug;
				case ApmLogLevel.Information: return NLogLevel.Info;
				case ApmLogLevel.Warning: return NLogLevel.Warn;
				case ApmLogLevel.Error: return NLogLevel.Error;
				case ApmLogLevel.Critical: return NLogLevel.Fatal;
				case ApmLogLevel.None: return NLogLevel.Off;
				default: return NLogLevel.Info;
			}
		}
	}
}
