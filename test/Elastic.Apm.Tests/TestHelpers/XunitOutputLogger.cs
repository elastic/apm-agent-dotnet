using Elastic.Apm.Logging;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class XunitOutputLogger : LineWriterToLoggerAdaptor
	{
		public XunitOutputLogger(ITestOutputHelper xUnitOutputHelper, LogLevel level = LogLevel.Trace)
			: base(new XunitOutputToLineWriterAdaptor(xUnitOutputHelper), level) { }
	}
}
