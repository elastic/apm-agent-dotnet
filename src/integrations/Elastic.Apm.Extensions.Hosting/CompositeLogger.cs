// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Logging;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.Extensions.Hosting;

internal sealed class CompositeLogger(TraceLogger traceLogger, IApmLogger logger) : IDisposable , IApmLogger
{
	public TraceLogger TraceLogger { get; } = traceLogger;
public IApmLogger ApmLogger { get; } = logger;

private bool _isDisposed;

public void Dispose() => _isDisposed = true;

public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
{
	if (_isDisposed)
		return;

	if (TraceLogger.IsEnabled(level))
		TraceLogger.Log(level, state, e, formatter);

	if (ApmLogger.IsEnabled(level))
		ApmLogger.Log(level, state, e, formatter);
}

public bool IsEnabled(LogLevel logLevel) => ApmLogger.IsEnabled(logLevel) || TraceLogger.IsEnabled(logLevel);


}
