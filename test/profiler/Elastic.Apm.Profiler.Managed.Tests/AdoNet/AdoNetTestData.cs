// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using System.Collections.Generic;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	public class AdoNetTestData : IEnumerable<object[]>
	{
		public const int DbRunnerExpectedTotalSpans = DbRunnerExpectedRunAllAsyncSpans + DbRunnerExpectedRunBaseTypesAsyncSpans;
		public const int DbRunnerExpectedRunAllAsyncSpans = 111;
		public const int DbRunnerExpectedRunBaseTypesAsyncSpans = 68;

		public const string OracleProviderSpanNameStart = "DECLARE";

		public IEnumerator<object[]> GetEnumerator()
		{
			// TODO: Add x64/x86 options. macOS and Linux do not support x86
			yield return new object[] { "net8.0" };
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
