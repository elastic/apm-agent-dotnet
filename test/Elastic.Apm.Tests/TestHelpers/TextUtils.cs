using System.IO;
using System.Text;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.TestHelpers
{
	public static class TextUtils
	{
		public const string Indentation = "    "; // 4 spaces
		public const char IndentationChar = ' '; // spaces
		public const int IndentationLength = 4; // spaces

		public static string AddIndentation(string input, string indentation = Indentation)
		{
			var resultBuilder = new StringBuilder(input.Length);
			using (var sr = new StringReader(input))
			{
				var isFirstLine = true;
				string line;
				while ((line = sr.ReadLine()) != null)
				{
					if (isFirstLine)
						isFirstLine = false;
					else
						resultBuilder.AppendLine();
					resultBuilder.Append(indentation);
					resultBuilder.Append(line);
				}
			}

			return resultBuilder.ToString();
		}

		public static string AddIndentation(string input, int indentationLevel) =>
			AddIndentation(input, Indentation.Repeat(indentationLevel));
	}
}
