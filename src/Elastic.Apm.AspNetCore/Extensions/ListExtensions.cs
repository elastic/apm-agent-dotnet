// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Elastic.Apm.AspNetCore.Extensions
{
	internal static class ListExtensions
	{
		/// <summary>
		/// Similar to <see cref="List{T}.Contains"/> but matches the string using
		/// a 'like' operator instead of an exact match
		/// </summary>
		/// <param name="list">The list in which to find the match</param>
		/// <param name="matchedString">The string to match</param>
		/// <returns></returns>
		public static bool ContainsLike(this List<string> list, string matchedString)
		{
			foreach (var str in list)
				if (str.IsLike(matchedString))
					return true;

			return false;
		}
	}
}
