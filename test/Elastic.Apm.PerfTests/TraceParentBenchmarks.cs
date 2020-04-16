using System.Diagnostics;
using BenchmarkDotNet.Attributes;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class TraceParentBenchmarks
	{
		[Benchmark]
		public void ParseTraceparentHeader()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var res = DistributedTracing.TraceContext.TryExtractTracingData(traceParent);
			Debug.WriteLine($"{res}");
		}
	}
}
