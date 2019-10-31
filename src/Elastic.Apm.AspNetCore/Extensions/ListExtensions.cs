using System.Collections.Generic;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class ListExtensions
	{
		/// <summary>
		/// Similar to List
		/// <T>
		/// .Contains but matches the string using a 'like' operator instead
		/// of an exact match
		/// </summary>
		/// <param name="list"></param>
		/// <param name="matchedString"></param>
		/// <returns></returns>
		public static bool ContainsLike(this List<string> list, string matchedString)
		{
			foreach (var str in list)
			{
				if (str.IsLike(matchedString))
					return true;
			}

			return false;
		}
	}
}
