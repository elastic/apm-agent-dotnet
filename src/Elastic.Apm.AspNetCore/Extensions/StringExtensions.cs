using System.Text.RegularExpressions;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class StringExtensions
	{
		/// <summary>
		/// Enables wildcard string matching
		/// </summary>
		/// <param name="pattern">The pattern used for matchinf</param>
		/// <param name="text">String being matched</param>
		/// <param name="caseSensitive">Weather to match with case sensitivy</param>
		/// <returns></returns>
		public static bool IsLike(this string pattern, string text, bool caseSensitive = false)
		{
			pattern = pattern.Replace(".", @"\.");
			pattern = pattern.Replace("?", ".");
			pattern = pattern.Replace("*", ".*?");
			pattern = pattern.Replace(@"\", @"\\");
			pattern = pattern.Replace(" ", @"\s");
			return new Regex(pattern, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase).IsMatch(text);
		}
	}
}
