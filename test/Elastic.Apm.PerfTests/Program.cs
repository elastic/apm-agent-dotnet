using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Elastic.Apm.Tests.Mocks;

namespace Elastic.Apm.PerfTests
{
	[MemoryDiagnoser]
	public class Program
	{
		static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<Program>();
		}

		[Benchmark]
		public void SimmpleTransactions10Spans()
		{
			var agent = new ApmAgent(new AgentComponents(payloadSender: new MockPayloadSender()));

			for (var i = 0; i < 100; i++)
			{
				agent.Tracer.CaptureTransaction("t", "t", (transaction) =>
				{
					for (var j = 0; j < 10; j++)
					{
						transaction.CaptureSpan("span", "perfSpan", () => { });
					}
				});
			}
		}
	}
}
