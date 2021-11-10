using System;
using Elastic.Apm;

namespace OpenTelemetrySample
{
	internal class Program
	{
		// ReSharper disable once ArrangeTypeMemberModifiers
		static void Main(string[] args)
		{
			Agent.Setup(new AgentComponents());
			new OTSamples().Sample1();
			new OTSamples().Sample2(Agent.Tracer);
			new OTSamples().Sample3(Agent.Tracer);
			new OTSamples().Sample4(Agent.Tracer);
			new OTSamples().SpanKindSample();

			Console.ReadKey();
		}
	}
}
