// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using BenchmarkDotNet.Attributes;
using Elastic.Apm.Config;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Utilities;

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

			_metricsCollector = new MetricsCollector(noopLogger, mockPayloadSender, new ConfigStore(new MockConfigSnapshot(noopLogger), noopLogger));
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
