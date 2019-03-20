using System;

namespace Elastic.Apm.DistributedTracing
{
	//00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01


	/// <summary>
	/// This is an implementation of the "https://w3c.github.io/distributed-tracing/report-trace-context.html#traceparent-field" w3c traceparent header draft.
	/// traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
	/// (_________)  () (______________________________) (______________) ()
	///      v       v                 v                        v         v
	/// Header name  Version        Trace-Id                Span-Id     Flags
	/// </summary>
	internal struct TraceParent
	{
		public byte[] TraceFlag { get; set; }

		public static string GetTraceParentVal(string traceId, string spanId)
			=> $"00-{traceId}-{spanId}-01";

		public static (string parent, string traceId) ParseTraceParentString(string traceParent)
		{
			var traceParentByte = traceParent.Substring(3, 32);
			var parentId = traceParent.Substring(36, 16);

			return (traceParentByte, parentId);
		}


		internal const string TraceParentHeaderName = "traceparent";

		private static byte[] StringToByteArray(string hex)
		{
			var NumberChars = hex.Length;
			var bytes = new byte[NumberChars / 2];
			for (var i = 0; i < NumberChars; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}
	}

	internal static class Consts
	{
		private static byte Version0 = 00000000;
	}
}
