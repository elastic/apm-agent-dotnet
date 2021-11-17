using System;
using Elastic.Apm;

namespace OpenTelemetrySample
{
	internal class Program
	{
		// ReSharper disable once ArrangeTypeMemberModifiers
		private static void Main(string[] args)
		{
			Agent.Setup(new AgentComponents());
			OTSamples.Sample1();
			OTSamples.Sample2(Agent.Tracer);
			OTSamples.Sample3(Agent.Tracer);
			OTSamples.Sample4(Agent.Tracer);
			OTSamples.SpanKindSample();

			Console.ReadKey();
		}
	}
}
