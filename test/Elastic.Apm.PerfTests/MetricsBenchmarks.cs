using BenchmarkDotNet.Attributes;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class MetricsBenchmarks
	{
		[Benchmark]
		public void CollectAllMetrics2X()
		{
			var noopLogger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();
			using (var collector = new MetricsCollector(noopLogger, mockPayloadSender, new MockConfigSnapshot(noopLogger)))
			{
				collector.CollectAllMetrics();
				collector.CollectAllMetrics();
			}
		}

		[Benchmark]
		public void CollectProcessTotalCpuTime2X()
		{
			var mockPayloadSender = new ProcessTotalCpuTimeProvider(new NoopLogger());

			mockPayloadSender.GetSamples();
			mockPayloadSender.GetSamples();
		}

		[Benchmark]
		public void CollectTotalCpuTime2X()
		{
			var systemTotalCpuProvider = new SystemTotalCpuProvider(new NoopLogger());

			systemTotalCpuProvider.GetSamples();
			systemTotalCpuProvider.GetSamples();
		}

		[Benchmark]
		public void CollectTotalAndFreeMemory2X()
		{
			var mockPayloadSender = new FreeAndTotalMemoryProvider(true, true);

			mockPayloadSender.GetSamples();
			mockPayloadSender.GetSamples();
		}

		[Benchmark]
		public void CollectWorkingSetAndVirMem2X()
		{
			var mockPayloadSender = new ProcessWorkingSetAndVirtualMemoryProvider(true, true);

			mockPayloadSender.GetSamples();
			mockPayloadSender.GetSamples();
		}

	}
}
