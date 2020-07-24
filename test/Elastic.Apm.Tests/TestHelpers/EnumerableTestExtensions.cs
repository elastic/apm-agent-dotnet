// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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
