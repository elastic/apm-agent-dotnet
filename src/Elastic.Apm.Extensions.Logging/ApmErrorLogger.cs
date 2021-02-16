// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Microsoft.Extensions.Logging;
using Elastic.Apm.Logging;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Elastic.Apm.Extensions.Logging
{
	/// <summary>
	/// Captures logs on error level as APM errors
	/// </summary>
	internal class ApmErrorLogger : ILogger
	{
		private readonly IApmAgent _agent;

		private readonly NullScope _nullScope;

		public ApmErrorLogger(IApmAgent agent) => (_agent, _nullScope) = (agent, new NullScope());

		public IDisposable BeginScope<TState>(TState state) =>
			_nullScope;

		public bool IsEnabled(LogLevel logLevel)
			=> logLevel >= LogLevel.Error;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (!Agent.IsConfigured) return;
			if (!IsEnabled(logLevel)) return;
			if (!_agent.ConfigurationReader.Enabled || !_agent.ConfigurationReader.Recording) return;

			var logLine = formatter(state, exception);
			var errorLog = new ErrorLog(logLine);

			if (_agent is ApmAgent apmAgent && exception != null)
			{
				errorLog.StackTrace = StacktraceHelper.GenerateApmStackTrace(exception, null, "CaptureErrorLogsAsApmError",
					apmAgent.ConfigurationReader, apmAgent.Components.ApmServerInfo);
			}

			errorLog.Level = logLevel.ToString();

			if (state is IEnumerable<KeyValuePair<string, object>> stateValues)
			{
				foreach (var item in stateValues)
				{
					if (item.Key == "{OriginalFormat}")
						errorLog.ParamMessage = item.Value.ToString();
				}
			}

			try
			{
				if (_agent.Tracer.CurrentSpan != null)
					_agent.Tracer.CurrentSpan.CaptureErrorLog(errorLog);
				else if (_agent.Tracer.CurrentTransaction != null)
					_agent.Tracer.CurrentTransaction.CaptureErrorLog(errorLog);
				else
					_agent.Tracer.CaptureErrorLog(errorLog);
			}
			catch(Exception e)
			{
				_agent.Logger.Warning()?.LogException(e, "Failed capturing APM Error based on log");
			}
		}

		private class NullScope : IDisposable
		{
			public void Dispose() { }
		}
	}
}
