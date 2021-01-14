// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.Utilities
{
	public class FlushingTextWriterToLineWriterAdaptor : ILineWriter
	{
		private readonly string _prefix;
		private readonly TextWriter _textWriter;

		public FlushingTextWriterToLineWriterAdaptor(TextWriter textWriter, string prefix = "")
		{
			_textWriter = textWriter;
			_prefix = prefix;
		}

		public void WriteLine(string text)
		{
			_textWriter.WriteLine(TextUtils.PrefixEveryLine(text, _prefix));
			_textWriter.Flush();
		}
	}
}
