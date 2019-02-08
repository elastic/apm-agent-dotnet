using System.Collections.Generic;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	public class TestLogger : AbstractLogger
	{
		public TestLogger() : base(LogLevelDefault) { }

		public TestLogger(LogLevel level) : base(level) { }

		public List<string> Lines { get; } = new List<string>();

		protected override void PrintLogLine(string logline) => Lines.Add(logline);
	}
}
