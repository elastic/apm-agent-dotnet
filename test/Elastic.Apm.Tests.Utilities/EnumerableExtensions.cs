// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.Utilities
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

		internal static Task ForEach<T>(this IEnumerable<T> source, Func<T, Task> asyncAction)
		{
			source.ThrowIfArgumentNull(nameof(source));
			asyncAction.ThrowIfArgumentNull(nameof(asyncAction));

			return ForEachImpl(source, asyncAction);
		}

		private static async Task ForEachImpl<T>(this IEnumerable<T> source, Func<T, Task> asyncAction)
		{
			// perf optimization. try to not use enumerator if possible
			if (source is IList<T> list)
			{
				for (int i = 0, count = list.Count; i < count; ++i)
					await asyncAction(list[i]);
			}
			else
			{
				foreach (var value in source)
					await asyncAction(value);
			}
		}

		internal static Task ForEachIndexed<T>(this IEnumerable<T> source, Func<T, int, Task> asyncAction)
		{
			source.ThrowIfArgumentNull(nameof(source));
			asyncAction.ThrowIfArgumentNull(nameof(asyncAction));

			return ForEachIndexedImpl(source, asyncAction);
		}

		private static async Task ForEachIndexedImpl<T>(this IEnumerable<T> source, Func<T, int, Task> asyncAction)
		{
			// perf optimization. try to not use enumerator if possible
			if (source is IList<T> list)
			{
				for (int i = 0, count = list.Count; i < count; ++i)
					await asyncAction(list[i], i);
			}
			else
			{
				var i = 0;
				foreach (var value in source)
				{
					await asyncAction(value, i);
					++i;
				}
			}
		}
	}
}
