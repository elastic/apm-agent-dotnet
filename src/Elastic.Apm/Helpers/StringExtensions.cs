using System.Text;

namespace Elastic.Apm.Helpers
{
	internal static class StringExtensions
	{
		private static string TrimToLength(this string input, int maxLength)
		{
			input.ThrowIfArgumentNull(nameof(input));

			if (input.Length > maxLength)
				input = $"{input.Substring(0, Consts.PropertyMaxLength - 3)}...";

			return input;
		}

		internal static string TrimToMaxLength(this string input) => input.TrimToLength(Consts.PropertyMaxLength);

		public static bool IsEmpty(this string input)
		{
			input.ThrowIfArgumentNull(nameof(input));

			return input.Length == 0;
		}

		public static string Repeat(this string input, int count)
		{
			input.ThrowIfArgumentNull(nameof(input));
			count.ThrowIfArgumentNegative(nameof(count));

			if (input.IsEmpty() || count == 0) return string.Empty;
			if (count == 1) return input;
			return new StringBuilder(input.Length * count).Insert(0, input, count).ToString();
		}
	}
}
