namespace Elastic.Apm.Helpers
{
	internal static class StringExtensions
	{
		private static string TrimToLength(this string input, int maxLength)
		{
			if (input.Length > maxLength)
				input = $"{input.Substring(0, Consts.PropertyMaxLength - 3)}...";

			return input;
		}

		internal static string TrimToMaxLength(this string input) => input.TrimToLength(Consts.PropertyMaxLength);
	}
}
