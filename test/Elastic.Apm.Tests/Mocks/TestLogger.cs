using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.Mocks
{
	internal class TestLogger : ConsoleLogger
	{
		private readonly StringWriter _writer;

		public TestLogger() : this(LogLevel.Error, new StringWriter()) { }

		public TestLogger(LogLevel level) : this(level, new StringWriter()) { }

		public TestLogger(LogLevel level, StringWriter writer) : base(level, writer, writer) => _writer = writer;

		public List<string> Lines => _writer.GetStringBuilder().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();
	}
}
