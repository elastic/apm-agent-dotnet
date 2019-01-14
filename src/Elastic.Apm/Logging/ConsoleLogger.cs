using System;
using Elastic.Apm.Config;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : AbstractLogger
	{
		protected ConsoleLogger(LogLevel level) : base(level) { }

		protected override void PrintLogline(string logline) => Console.WriteLine(logline);

		public static ConsoleLogger Instance { get; } = new ConsoleLogger(LogLevelDefault);
	}
}
