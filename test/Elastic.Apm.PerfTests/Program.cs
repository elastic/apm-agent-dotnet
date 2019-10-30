using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class Program
	{
		public static void Main(string[] args) => BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);


		private string str;
		public Program()
		{
			var random = new Random();
			for (var i = 0; i < 1000; i++)
			{
				str += random.Next(10).ToString();
			}

		}

		[Benchmark]
		public void MatcherVerbatimCaseSensitive()
		{
			var matcher = new WildcardMatcher.VerbatimMatcher(str, true);
			var res = matcher.Matches(str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherVerbatimCaseInsensitive()
		{
			var matcher = new WildcardMatcher.VerbatimMatcher(str, false);
			var res = matcher.Matches(str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherSimpleCaseSensitive()
		{
			var matcher = new WildcardMatcher.SimpleWildcardMatcher(str, false, false, false);
			var res = matcher.Matches(str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherSimpleCaseInsensitive()
		{
			var matcher = new WildcardMatcher.SimpleWildcardMatcher(str, false, false, true);
			var res = matcher.Matches(str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void SimpleTransactions10Spans()
		{
			var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender()));

			for (var i = 0; i < 100; i++)
			{
				agent.Tracer.CaptureTransaction("transaction", "perfTransaction", transaction =>
				{
					for (var j = 0; j < 10; j++) transaction.CaptureSpan("span", "perfSpan", () => { });
				});
			}
		}

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
			var mockPayloadSender = new FreeAndTotalMemoryProvider();

			mockPayloadSender.GetSamples();
			mockPayloadSender.GetSamples();
		}

		[Benchmark]
		public void CollectWorkingSetAndVirMem2X()
		{
			var mockPayloadSender = new ProcessWorkingSetAndVirtualMemoryProvider();

			mockPayloadSender.GetSamples();
			mockPayloadSender.GetSamples();
		}

		[Benchmark]
		public void Simple100Transaction10Spans()
		{
			var noopLogger = new NoopLogger();
			var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: noopLogger,
				configurationReader: new MockConfigSnapshot(noopLogger, spanFramesMinDurationInMilliseconds: "-1ms", stackTraceLimit: "10")));

			for (var i = 0; i < 100; i++)
			{
				agent.Tracer.CaptureTransaction("transaction", "perfTransaction", transaction =>
				{
					for (var j = 0; j < 10; j++) transaction.CaptureSpan("span", "perfSpan", () => { });
				});
			}
		}

		[Benchmark]
		public void DebugLogSimple100Transaction10Spans()
		{
			var testLogger = new PerfTestLogger(LogLevel.Debug);
			var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: testLogger,
				configurationReader: new MockConfigSnapshot(testLogger, "Debug")));


			for (var i = 0; i < 100; i++)
			{
				agent.Tracer.CaptureTransaction("transaction", "perfTransaction", transaction =>
				{
					for (var j = 0; j < 10; j++) transaction.CaptureSpan("span", "perfSpan", () => { });
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
