using System;
using System.Runtime.CompilerServices;
using Elastic.Apm.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Elastic.Apm.Logging.LogLevel;

[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]
[assembly:
	InternalsVisibleTo(
		"Elastic.Apm.AspNetCore.Static.Tests, PublicKey=002400000480000094000000060200000024000052534131000400000100010051df3e4d8341d66c6dfbf35b2fda3627d08073156ed98eef81122b94e86ef2e44e7980202d21826e367db9f494c265666ae30869fb4cd1a434d171f6b634aa67fa8ca5b9076d55dc3baa203d3a23b9c1296c9f45d06a45cf89520bef98325958b066d8c626db76dd60d0508af877580accdd0e9f88e46b6421bf09a33de53fe1")]

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
