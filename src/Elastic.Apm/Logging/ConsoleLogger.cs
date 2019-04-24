using System;
using System.IO;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : AsyncLineWriterLogger
	{
		internal ConsoleLogger(LogLevel level, TextWriter standardOut = null, TextWriter errorOut = null)
			: base(level,
				new TextWriterToAsyncLineWriterAdapter(standardOut ?? Console.Out),
				new TextWriterToAsyncLineWriterAdapter(errorOut ?? Console.Error)) { }

		protected internal static LogLevel DefaultLogLevel { get; } = LogLevel.Error;

		public static ConsoleLogger Instance { get; } = new ConsoleLogger(DefaultLogLevel);

		public static ConsoleLogger LoggerOrDefault(LogLevel? level)
		{
			if (level.HasValue && level.Value != DefaultLogLevel)
				return new ConsoleLogger(level.Value);

			return Instance;
		}
	}
}
