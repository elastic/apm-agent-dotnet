using System;

namespace Elastic.Apm.Logging
{
	internal class ConsoleLogger : AbstractLogger
	{
		public ConsoleLogger() => Prefix = string.Empty;

		public ConsoleLogger(string prefix) => Prefix = prefix;

		protected override void PrintLogline(string logline) => Console.WriteLine(logline);
	}
}
