using System;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : AbstractLogger
	{
		private ConsoleLogger(LogLevel level) : base(level) { }

		protected override void PrintLogLine(string logLine) => Console.WriteLine(logLine);

		internal static ConsoleLogger Instance { get; } = new ConsoleLogger(LogLevelDefault);
	}
}
