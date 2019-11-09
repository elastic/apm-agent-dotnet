using System.IO;
using System.Text;

namespace Elastic.Apm.Helpers
{
	internal class StringWriterPool : ObjectPool<StringWriter>
	{
		internal StringWriterPool(int amount, int initialCharactersAmount, int charactersLimit)
			: base(amount,
				() => new StringWriter(new StringBuilder(initialCharactersAmount)),
				writer =>
				{
					var builder = writer.GetStringBuilder();
					builder.Length = 0;
					if (builder.Capacity > charactersLimit) builder.Capacity = charactersLimit;
				}) { }
	}
}
