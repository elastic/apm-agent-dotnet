using System.IO;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class FlushingTextWriterToLoggerAdaptor : LineWriterToLoggerAdaptor
	{
		public FlushingTextWriterToLoggerAdaptor(TextWriter textWriter, LogLevel level = LogLevel.Information)
			: base(new FlushingTextWriterToLineWriterAdaptor(textWriter), level) { }
	}
}
