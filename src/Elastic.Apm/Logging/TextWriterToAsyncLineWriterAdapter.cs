using System.IO;
using System.Threading.Tasks;

namespace Elastic.Apm.Logging
{
	internal class TextWriterToAsyncLineWriterAdapter : IAsyncLineWriter
	{
		private readonly TextWriter _textWriter;

		internal TextWriterToAsyncLineWriterAdapter(TextWriter textWriter) => _textWriter = textWriter;

		public Task WriteLineAsync(string line) => _textWriter.WriteLineAsync(line);
	}
}
