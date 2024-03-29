// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Text;

namespace Elastic.Apm.Helpers
{
	internal static class TextUtils
	{
		public const string Indentation = "    "; // 4 spaces
		public const char IndentationChar = ' '; // spaces
		public const int IndentationLength = 4; // spaces

		// The order in endOfLines is important because we need to check longer sequences first
		private static readonly string[] EndOfLineCharSequences = { "\r\n", "\n", "\r" };

		internal static string PrefixEveryLine(string input, string prefix = Indentation)
		{
			input.ThrowIfArgumentNull(nameof(input));

			// We treat empty input as a special case because StringReader doesn't return it as an empty line
			if (input.IsEmpty())
				return prefix;

			var resultBuilder = new StringBuilder(input.Length);
			using (var stringReader = new StringReader(input))
			{
				var isFirstLine = true;
				string line;
				while ((line = stringReader.ReadLine()) != null)
				{
					if (isFirstLine)
						isFirstLine = false;
					else
						resultBuilder.AppendLine();
					resultBuilder.Append(prefix);
					resultBuilder.Append(line);
				}
			}

			// Since lines returned by StringReader exclude newline characters it's possible that the last line had newline at the end
			// but we didn't append it

			foreach (var endOfLineSeq in EndOfLineCharSequences)
			{
				if (!input.EndsWith(endOfLineSeq))
					continue;

				resultBuilder.Append(endOfLineSeq);
				break;
			}

			return resultBuilder.ToString();
		}

		internal static string Indent(this string input, string indentation = Indentation) => PrefixEveryLine(input, indentation);

		internal static string Indent(this string input, int indentationLevel) =>
			Indent(input, Indentation.Repeat(indentationLevel));

		internal static bool IsHex(char c) =>
			('0' <= c && c <= '9') ||
			('a' <= c && c <= 'f') ||
			('A' <= c && c <= 'F');

		internal static bool IsLatinLetter(char c) =>
			('a' <= c && c <= 'z') ||
			('A' <= c && c <= 'Z');
	}
}
