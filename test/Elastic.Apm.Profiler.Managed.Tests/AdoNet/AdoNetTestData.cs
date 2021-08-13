// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	public class AdoNetTestData : IEnumerable<object[]>
	{
		public const int DbRunnerExpectedTotalSpans = DbRunnerExpectedRunAllAsyncSpans + DbRunnerExpectedRunBaseTypesAsyncSpans;
		public const int DbRunnerExpectedRunAllAsyncSpans = 111;
		public const int DbRunnerExpectedRunBaseTypesAsyncSpans = 68;

		public IEnumerator<object[]> GetEnumerator()
		{
			// TODO: Add x64/x86 options. macOS and Linux do not support x86

			yield return new object[] { "net5.0" };
			yield return new object[] { "netcoreapp3.1" };

			// macOS only supports netcoreapp3.1 and up
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				yield return new object[] { "netcoreapp3.0" };
				yield return new object[] { "netcoreapp2.1" };
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				yield return new object[] { "net461" };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
