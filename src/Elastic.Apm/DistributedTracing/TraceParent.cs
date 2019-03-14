using System;

namespace Elastic.Apm.DistributedTracing
{
	internal struct TraceParent
	{
		public byte[] TraceFlag { get; set; }

		public static string GetTraceParentVal(string traceId, string spanId)
		=> $"00-{traceId}-{spanId}-01";


		internal const string TraceParentHeaderName = "traceparent";
	}

	internal static class Consts
	{
		private static byte Version0 = 00000000;
	}
}
