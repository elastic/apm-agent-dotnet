using System.IO;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class FlushingTextWriterToLineWriterAdaptor : ILineWriter
	{
		private readonly TextWriter _textWriter;

		public FlushingTextWriterToLineWriterAdaptor(TextWriter textWriter) => _textWriter = textWriter;

		public void WriteLine(string line)
		{
			_textWriter.WriteLine(line);
			_textWriter.Flush();
		}
	}
}
