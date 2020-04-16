using BenchmarkDotNet.Attributes;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.PerfTests
{
	/// <summary>
	/// Benchmarks related to tracer (like starting a transaction, span, etc.)
	/// </summary>
	[MemoryDiagnoser]
	public class TracerBenchmarks
	{
		private ApmAgent _agent;

		[GlobalSetup(Target = nameof(SimpleTransactionsWith1SpansWithStackTrace))]
		public void SetupWithStackTraceForAllSpans()
			=> _agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(),
				configurationReader: new MockConfigSnapshot(spanFramesMinDurationInMilliseconds: "-1ms")));

		[GlobalSetup(Target = nameof(SimpleTransactionsWith1SpansWithoutStackTrace))]
		public void SetupWithTurnedOffStackTrace()
			=> _agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(),
				configurationReader: new MockConfigSnapshot(spanFramesMinDurationInMilliseconds: "0ms")));

		[GlobalSetup(Target = nameof(Simple100Transaction10Spans))]
		public void DefaultAgentSetup()
		{
			var noopLogger = new NoopLogger();
			_agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: noopLogger,
				configurationReader: new MockConfigSnapshot(noopLogger)));
		}

		[GlobalSetup(Target = nameof(DebugLogSimpleTransaction10Spans))]
		public void DebugAgentSetup()
		{
			var testLogger = new PerfTestLogger(LogLevel.Debug);
			_agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(), logger: testLogger,
				configurationReader: new MockConfigSnapshot(testLogger, "Debug")));
		}

		[Benchmark]
		public void SimpleTransactionsWith1SpansWithStackTrace()
			=> _agent.Tracer.CaptureTransaction("transaction", "perfTransaction",
				transaction => { transaction.CaptureSpan("span", "perfSpan", () => { }); });

		[Benchmark]
		public void SimpleTransactionsWith1SpansWithoutStackTrace()
			=> _agent.Tracer.CaptureTransaction("transaction", "perfTransaction",
				transaction => { transaction.CaptureSpan("span", "perfSpan", () => { }); });

		[GlobalCleanup]
		public void GlobalCleanup() => _agent.Dispose();

		[Benchmark]
		public void Simple100Transaction10Spans()
			=> _agent.Tracer.CaptureTransaction("transaction", "perfTransaction", transaction =>
			{
				for (var j = 0; j < 10; j++) transaction.CaptureSpan("span", "perfSpan", () => { });
			});

		[Benchmark]
		public void DebugLogSimpleTransaction10Spans()
			=> _agent.Tracer.CaptureTransaction("transaction", "perfTransaction", transaction =>
			{
				for (var j = 0; j < 10; j++) transaction.CaptureSpan("span", "perfSpan", () => { });
			});
	}
}
