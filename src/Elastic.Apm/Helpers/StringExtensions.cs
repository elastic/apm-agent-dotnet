using System;
using System.Text;

namespace Elastic.Apm.Helpers
{
	internal static class StringExtensions
	{
		internal static bool IsEmpty(this string input)
		{
			input.ThrowIfArgumentNull(nameof(input));

			return input.Length == 0;
		}

		internal static string Repeat(this string input, int count)
		{
			input.ThrowIfArgumentNull(nameof(input));
			count.ThrowIfArgumentNegative(nameof(count));

			if (input.IsEmpty() || count == 0) return string.Empty;
			if (count == 1) return input;

			return new StringBuilder(input.Length * count).Insert(0, input, count).ToString();
		}

		// Credit: https://stackoverflow.com/a/444818/973581
		internal static bool ContainsOrdinalIgnoreCase(this string thisObj, string subStr) =>
			thisObj.IndexOf(subStr, StringComparison.OrdinalIgnoreCase) >= 0;
	}
}
