// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elastic.Apm.Helpers
{
	internal static class StringExtensions
	{
		internal static string NotNull(this string s) => s ?? string.Empty;

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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool ContainsOrdinalIgnoreCase(this string s, string value) =>
			s.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;

		internal static string ToLog(this string thisObj) => "`" + thisObj + "'";

		internal const string Ellipsis = "…";

		/// <summary>
		/// Truncates the string to a given length, if longer than the length
		/// </summary>
		internal static string Truncate(this string input, int length = Consts.PropertyMaxLength)
		{
			if (input is null)
				return null;

			if (input.Length <= length) return input;

			if (length <= 5) return input.Substring(0, length);

			return $"{input.Substring(0, length - Ellipsis.Length)}{Ellipsis}";
		}
	}
}
