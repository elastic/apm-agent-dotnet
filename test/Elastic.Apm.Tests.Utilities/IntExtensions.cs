// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;

namespace Elastic.Apm.Tests.Utilities
{
	public static class IntExtensions
	{
		public static void Repeat(this int repeatCount, Action action)
		{
			for (var i = 0; i < repeatCount; ++i)
				action();
		}

		public static void Repeat(this int repeatCount, Action<int> action)
		{
			for (var i = 0; i < repeatCount; ++i)
				action(i);
		}

		public static async Task Repeat(this int repeatCount, Func<int, Task> action)
		{
			for (var i = 0; i < repeatCount; ++i)
				await action(i);
		}
	}
}
