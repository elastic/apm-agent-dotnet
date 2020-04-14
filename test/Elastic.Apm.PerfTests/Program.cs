using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Mocks;
using Elastic.CommonSchema.BenchmarkDotNetExporter;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class Program
	{
		public static void Main(string[] args)
		{
			_str = string.Empty;
			var random = new Random();
			for (var i = 0; i < 1000; i++) _str += random.Next(10).ToString();

			var options = new ElasticsearchBenchmarkExporterOptions("http://localhost:9200");
			var exporter = new ElasticsearchBenchmarkExporter(options);

			var config =  DefaultConfig.Instance.With(exporter);
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
		}

		private static string _str;

		[Benchmark]
		public void MatcherVerbatimCaseSensitive()
		{
			var matcher = new WildcardMatcher.VerbatimMatcher(_str, true);
			var res = matcher.Matches(_str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherVerbatimCaseInsensitive()
		{
			var matcher = new WildcardMatcher.VerbatimMatcher(_str, false);
			var res = matcher.Matches(_str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherSimpleCaseSensitive()
		{
			var matcher = new WildcardMatcher.SimpleWildcardMatcher(_str, false, false, false);
			var res = matcher.Matches(_str);
			Debug.WriteLine(res);
		}

		[Benchmark]
		public void MatcherSimpleCaseInsensitive()
		{
			var matcher = new WildcardMatcher.SimpleWildcardMatcher(_str, false, false, true);
			var res = matcher.Matches(_str);
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

		[Benchmark]
		public void Simple100Transaction10Spans()
		{
			var noopLogger = new NoopLogger();

			using (var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: noopLogger,
				configurationReader: new MockConfigSnapshot(noopLogger))))
			{
				for (var i = 0; i < 100; i++)
				{
					agent.Tracer.CaptureTransaction("transaction", "perfTransaction", transaction =>
					{
						for (var j = 0; j < 10; j++) transaction.CaptureSpan("span", "perfSpan", () => { });
					});
				}
			}
		}

		[Benchmark]
		public void DebugLogSimple100Transaction10Spans()
		{
			var testLogger = new PerfTestLogger(LogLevel.Debug);

			using (var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: testLogger,
				configurationReader: new MockConfigSnapshot(testLogger, "Debug"))))
			{
				for (var i = 0; i < 100; i++)
				{
					agent.Tracer.CaptureTransaction("transaction", "perfTransaction", transaction =>
					{
						for (var j = 0; j < 10; j++) transaction.CaptureSpan("span", "perfSpan", () => { });
					});
				}
			}
		}

		[Benchmark]
		public void ParseTraceparentHeader()
		{
			const string traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

			var res = DistributedTracing.TraceContext.TryExtractTracingData(traceParent);
			Debug.WriteLine($"{res}");
		}
	}
}
