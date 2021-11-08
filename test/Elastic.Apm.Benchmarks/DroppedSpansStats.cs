// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using BenchmarkDotNet.Attributes;
using Elastic.Apm.Tests.Utilities;

namespace Elastic.Apm.Benchmarks
{
	[MemoryDiagnoser]
	public class DroppedSpansStats
	{
		private ApmAgent _agent;

		[GlobalSetup]
		public void SetupWithLowMaxSpans()
			=> _agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender(),
				configurationReader: new MockConfiguration(transactionMaxSpans: "1")));

		[Benchmark]
		public void Test10Spans() => _agent.Tracer.CaptureTransaction("foo", "bar", t =>
									 {
										 for (var i = 0; i < 10; i++)
											 t.CaptureSpan("foo", "bar", () => { });
									 });
	}
}
