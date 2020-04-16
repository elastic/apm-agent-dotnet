using BenchmarkDotNet.Attributes;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class MetricsBenchmarks
	{
		private MetricsCollector _metricsCollector;

		[GlobalSetup(Target = nameof(CollectAllMetrics2X))]
		public void SetUpForAllMetrics()
		{
			var noopLogger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();

			_metricsCollector = new MetricsCollector(noopLogger, mockPayloadSender, new MockConfigSnapshot(noopLogger));
		}

		[GlobalCleanup(Target = nameof(CollectAllMetrics2X))]
		public void CleanUpForAllMetrics()
		 => _metricsCollector?.Dispose();

		[Benchmark]
		public void CollectAllMetrics2X()
		{
			_metricsCollector.CollectAllMetrics();
			_metricsCollector.CollectAllMetrics();
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
