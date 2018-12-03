using System;

namespace Elastic.Apm.Logging
{
    public class ConsoleLogger : AbstractLogger
    {
        public ConsoleLogger() => Prefix = String.Empty;
        public ConsoleLogger(String prefix) => Prefix = prefix;

        protected override void PrintLogline(string logline) => Console.WriteLine(logline);
    }
}
