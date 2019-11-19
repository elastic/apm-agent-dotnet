using System;
using System.Collections.Generic;
using System.Linq;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class EnumerableTestExtensions
	{
		internal static IEnumerable<ValueTuple<int, T>> ZipWithIndex<T>(this IEnumerable<T> source, int start = 0) =>
			Enumerable.Range(start, start == 0 ? int.MaxValue - 1 : int.MaxValue - start + 1).Zip(source, (i, t) => (i, t));
	}
}
