// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Microsoft.Extensions.Logging;

namespace Elastic.Apm.Extensions.Logging
{
	public class ElasticApmErrorLogger : ILogger
	{
		private readonly IApmAgent _agent;

		public ElasticApmErrorLogger(IApmAgent agent) => _agent = agent;

		public IDisposable BeginScope<TState>(TState state) =>
			null;

		public bool IsEnabled(LogLevel logLevel)
			=> logLevel >= LogLevel.Error;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			if (!Agent.IsConfigured) return;
			if (logLevel < LogLevel.Error) return;

			var logLine = formatter(state, exception);
			var logOnError = new ErrorLog(logLine);

			if (_agent is ApmAgent apmAgent && exception != null)
			{
				logOnError.StackTrace = StacktraceHelper.GenerateApmStackTrace(exception, null, "CaptureErrorLogsAsApmError",
					apmAgent.ConfigurationReader, apmAgent.Components.ApmServerInfo);
			}

			logOnError.Level = logLevel.ToString();

			if (state is IEnumerable<KeyValuePair<string, object>> stateValues)
			{
				foreach (var item in stateValues)
				{
					if (item.Key == "{OriginalFormat}") logOnError.ParamMessage = item.Value.ToString();
				}
			}

			if (_agent.Tracer.CurrentSpan != null)
				_agent.Tracer.CurrentSpan.CaptureLogAsError(logOnError);
			else if (_agent.Tracer.CurrentTransaction != null)
				_agent.Tracer.CurrentTransaction.CaptureLogAsError(logOnError);
			else
				_agent.Tracer.CaptureLogAsError(logOnError);
		}
	}
}
