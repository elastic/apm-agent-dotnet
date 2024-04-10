// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.Extensions.Hosting;

internal sealed class NetCoreLogger : IApmLogger
{
	private readonly ILogger _logger;

	public NetCoreLogger(ILoggerFactory loggerFactory) => _logger = loggerFactory?.CreateLogger("Elastic.Apm") ?? throw new ArgumentNullException(nameof(loggerFactory));

	public bool IsEnabled(LogLevel level) => _logger.IsEnabled(Convert(level));

	public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter) =>
		_logger.Log(Convert(level), new EventId(), state, e, formatter);

	private static Microsoft.Extensions.Logging.LogLevel Convert(LogLevel logLevel) =>
		logLevel switch
		{
			LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
			LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
			LogLevel.Information => Microsoft.Extensions.Logging.LogLevel.Information,
			LogLevel.Warning => Microsoft.Extensions.Logging.LogLevel.Warning,
			LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
			LogLevel.Critical => Microsoft.Extensions.Logging.LogLevel.Critical,
			_ => Microsoft.Extensions.Logging.LogLevel.None,
		};

	internal static IApmLogger GetApmLogger(IServiceProvider serviceProvider) =>
		serviceProvider.GetService(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory
			? new NetCoreLogger(loggerFactory)
			: ConsoleLogger.Instance;
}
