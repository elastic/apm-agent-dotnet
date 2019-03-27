using System;
using Elastic.Apm.Logging;

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

		private const int TraceParentLength = 55;
		private const byte FlagRecorded = 1; // 00000001

		public static bool ValidateTraceParentValue(string traceParentValue, IApmLogger logger)
		{
			if (traceParentValue.Length != 55)
			{
				logger.Warning()
					?.Log("Traceparent contains invalid length, expected: {ExpectedTraceParentLength}, got: {GotLength}", TraceParentLength,
						traceParentValue.Length);
				return false;
			}

			if (!traceParentValue.StartsWith("00-"))
			{
				logger.Warning()
					?.Log("Only version 00 of the traceparent header is supported, but was {GotVersion}", traceParentValue.Substring(0, 3));
				return false;
			}

			// ReSharper disable once InvertIf
			if (traceParentValue[35] != '-' || traceParentValue[52] != '-')
			{
				logger.Warning()?.Log("Invalid traceparent format, got: {TraceParentValue}", traceParentValue);
				return false;
			}

			return true;
		}

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
}
