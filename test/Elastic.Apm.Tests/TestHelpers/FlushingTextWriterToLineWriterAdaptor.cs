using System.IO;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class FlushingTextWriterToLineWriterAdaptor : ILineWriter
	{
		private readonly TextWriter _textWriter;
		private readonly string _prefix;

		public FlushingTextWriterToLineWriterAdaptor(TextWriter textWriter, string prefix = "")
		{
			_textWriter = textWriter;
			_prefix = prefix;
		}

		public void WriteLine(string line)
		{
			_textWriter.WriteLine(TextUtils.PrefixEveryLine(line, _prefix));
			_textWriter.Flush();
		}
	}
}
