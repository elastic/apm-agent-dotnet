using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class Program
	{
		public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

		[Benchmark]
		public void SimpleTransactions10Spans()
		{
			var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender()));

			for (var i = 0; i < 100; i++)
			{
				agent.Tracer.CaptureTransaction("transaction", "perfTransaction", (transaction) =>
				{
					for (var j = 0; j < 10; j++)
					{
						transaction.CaptureSpan("span", "perfSpan", () => { });
					}
				});
			}
		}

		[Benchmark]
		public void CollectAllMetrics2X()
		{
			var noopLogger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();
			using (var collector = new MetricsCollector(noopLogger, mockPayloadSender, new TestAgentConfigurationReader(noopLogger)))
			{
				collector.CollectAllMetrics();
				collector.CollectAllMetrics();
			}
		}

		[Benchmark]
		public void CollectProcessTotalCpuTime2X()
		{
			var noopLogger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();
			using (var collector = new MetricsCollector(noopLogger, mockPayloadSender, new TestAgentConfigurationReader(noopLogger)))
			{
				collector.GetProcessTotalCpuTime();
				collector.GetProcessTotalCpuTime();
			}
		}

		[Benchmark]
		public void CollectTotalCpuTime2X()
		{
			var noopLogger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();
			using (var collector = new MetricsCollector(noopLogger, mockPayloadSender, new TestAgentConfigurationReader(noopLogger)))
			{
				collector.GetSystemTotalCpuTime();
				collector.GetSystemTotalCpuTime();
			}
		}

		[Benchmark]
		public void CollectTotalAndFreeMemory2X()
		{
			var noopLogger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();
			using (var collector = new MetricsCollector(noopLogger, mockPayloadSender, new TestAgentConfigurationReader(noopLogger)))
			{
				collector.GetTotalAndFreeMemoryMemory();
				collector.GetTotalAndFreeMemoryMemory();
			}
		}

		[Benchmark]
		public void CollectWorkingSetAndVirMem2X()
		{
			var noopLogger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();
			using (var collector = new MetricsCollector(noopLogger, mockPayloadSender, new TestAgentConfigurationReader(noopLogger)))
			{
				collector.GetProcessWorkingSetAndVirtualMemory();
				collector.GetProcessWorkingSetAndVirtualMemory();
			}
		}

		[Benchmark]
		public void DebugLogSimple100Transaction10Spans()
		{
			var testLogger = new PerfTestLogger(LogLevel.Debug);
			var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: testLogger,
				configurationReader: new TestAgentConfigurationReader(testLogger, logLevel: "Debug")));


			for (var i = 0; i < 100; i++)
			{
				agent.Tracer.CaptureTransaction("transaction", "perfTransaction", (transaction) =>
				{
					for (var j = 0; j < 10; j++)
					{
						transaction.CaptureSpan("span", "perfSpan", () => { });
					}
				});
			}
		}

		[Benchmark]
		public void ParseTraceparentHeader()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var res = TraceParent.TryExtractTraceparent(traceParent);
			Debug.WriteLine($"{res}");
		}
	}
}
