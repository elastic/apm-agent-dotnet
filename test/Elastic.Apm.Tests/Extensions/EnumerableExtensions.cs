using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.Extensions
{
	/// <summary>
	/// Credit: http://source.roslyn.io/#microsoft.codeanalysis/InternalUtilities/EnumerableExtensions.cs
	/// </summary>
	internal static class EnumerableExtensions
	{
		internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			source.ThrowIfArgumentNull(nameof(source));
			action.ThrowIfArgumentNull(nameof(action));

			// perf optimization. try to not use enumerator if possible
			if (source is IList<T> list)
			{
				for (int i = 0, count = list.Count; i < count; ++i)
					action(list[i]);
			}
			else
			{
				foreach (var value in source)
					action(value);
			}
		}

		internal static void ForEachIndexed<T>(this IEnumerable<T> source, Action<T, int> action)
		{
			source.ThrowIfArgumentNull(nameof(source));
			action.ThrowIfArgumentNull(nameof(action));

			// perf optimization. try to not use enumerator if possible
			if (source is IList<T> list)
			{
				for (int i = 0, count = list.Count; i < count; ++i)
					action(list[i], i);
			}
			else
			{
				var i = 0;
				foreach (var value in source)
				{
					action(value, i);
					++i;
				}
			}
		}
	}
}
