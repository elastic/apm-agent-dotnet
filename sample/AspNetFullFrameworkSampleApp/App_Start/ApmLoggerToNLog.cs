using System;
using Elastic.Apm.Logging;
using NLog;
using NLogLevel = NLog.LogLevel;
using ApmLogLevel = Elastic.Apm.Logging.LogLevel;

namespace AspNetFullFrameworkSampleApp
{
	internal class ApmLoggerToNLog : IApmLogger
	{
		public bool IsEnabled(ApmLogLevel level) => true;

		public void Log<TState>(ApmLogLevel apmLogLevel, TState state, Exception e, Func<TState, Exception, string> formatter)
		{
			var nlogger = LogManager.GetLogger("");

			var message = formatter(state, e);
			if (e == null)
				nlogger.Log(ConvertLevel(apmLogLevel), message);
			else
				nlogger.Log(ConvertLevel(apmLogLevel), e, message);
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
