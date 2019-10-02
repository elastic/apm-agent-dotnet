using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Elastic.Apm.Helpers
{
	internal static class CollectionExtensions
	{
		internal static bool IsEmpty<T>(this T[] thisObj) => thisObj.Length == 0;

		internal static bool IsEmpty<T>(this IEnumerable<T> thisObj) => ! thisObj.Any();

		internal static bool IsEmpty<T>(this IReadOnlyCollection<T> thisObj) => thisObj.Count == 0;

		internal static bool IsEmpty(this ICollection thisObj) => thisObj.Count == 0;

		internal static bool IsEmpty<T>(this ICollection<T> thisObj) => thisObj.Count == 0;

		internal static bool IsEmpty<T>(this IList<T> thisObj) => thisObj.Count == 0;

		internal static bool IsEmpty<T>(this List<T> thisObj) => thisObj.Count == 0;

		internal static bool IsEmpty<TKey, TValue>(this IDictionary<TKey, TValue> thisObj) => thisObj.Count == 0;

		internal static bool IsEmpty<TKey, TValue>(this Dictionary<TKey, TValue> thisObj) => thisObj.Count == 0;

		internal static bool IsEmpty<T>(this Queue<T> thisObj) => thisObj.Count == 0;
	}
}
