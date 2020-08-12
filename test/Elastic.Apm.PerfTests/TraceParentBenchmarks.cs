// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.DistributedTracing;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class TraceParentBenchmarks
	{
		[Benchmark]
		public void ParseTraceparentHeader()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var res = TraceContext.TryExtractTracingData(traceParent);
			Debug.WriteLine($"{res}");
		}
	}
}
