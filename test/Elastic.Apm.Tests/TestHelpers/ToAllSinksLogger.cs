using System;
using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class ToAllSinksLogger : LineWriterToLoggerAdaptor
	{
		public ToAllSinksLogger(ITestOutputHelper xUnitOutputHelper, LogLevel level = LogLevel.Trace)
			: base(
				new SplittingLineWriter(
					new SystemDiagnosticsTraceLineWriter("<Elastic APM .NET Tests> "),
					new FlushingTextWriterToLineWriterAdaptor(Console.Out),
					new XunitOutputToLineWriterAdaptor(xUnitOutputHelper))
				, level) { }
	}
}
