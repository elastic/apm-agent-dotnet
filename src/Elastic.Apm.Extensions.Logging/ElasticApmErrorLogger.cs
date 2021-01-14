// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Extensions.Logging
{
	public class ElasticApmErrorLogger : ILogger
	{
		public IDisposable BeginScope<TState>(TState state) =>
			null;

		private readonly IApmAgent _agent;

		public ElasticApmErrorLogger(IApmAgent agent) => _agent = agent;

		public bool IsEnabled(LogLevel logLevel)
			=> logLevel >= LogLevel.Error;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			//if (_agent is ApmAgent apmAgent && !apmAgent.IsConfigured) return;
			if (logLevel < LogLevel.Error) return;

			//TODO: do not capture agent errors as APM error


			var logLine = formatter(state, exception);
			var logOnError = new LogOnError(logLine);

			if (_agent is ApmAgent apmAgent && exception != null)
				logOnError.StackTrace = StacktraceHelper.GenerateApmStackTrace(exception, null, "CaptureErrorLogsAsApmError",
					apmAgent.ConfigurationReader, apmAgent.Components.ApmServerInfo);

			logOnError.Level = logLevel.ToString();
			if (state is IEnumerable<KeyValuePair<string, object>> stateValues)
			{
				foreach (var item in stateValues)
				{
					if (item.Key == "{OriginalFormat}")
					{
						logOnError.ParamMessage = item.Value.ToString();
					}
				}
			}

			if (_agent.Tracer.CurrentSpan != null)
			{
				(_agent.Tracer.CurrentSpan as Span)?.CaptureLogError(logOnError);
			}
			else if (_agent.Tracer.CurrentTransaction != null)
			{
				//(Agent.Tracer.CurrentTransaction as Transaction)?.CaptureLogError(logOnError);
			}
		}
	}
}
