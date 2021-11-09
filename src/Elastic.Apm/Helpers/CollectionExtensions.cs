// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Elastic.Apm.Helpers
{
	internal static class CollectionExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static bool IsEmpty<T>(this List<T> thisObj) => thisObj.Count == 0;
	}
}
