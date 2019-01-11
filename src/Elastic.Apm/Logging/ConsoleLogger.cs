using System;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : AbstractLogger
	{
		protected ConsoleLogger(LogLevel level) : base(level) { }

		protected override void PrintLogline(string logline) => Console.WriteLine(logline);
	}
	internal class NoopLogger : AbstractLogger
	{
		protected NoopLogger() : base(LogLevelDefault) { }

		protected override void PrintLogline(string logline) { }

		public static NoopLogger Instance { get; } = new NoopLogger();
	}
}
