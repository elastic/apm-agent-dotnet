using System;
using Elastic.Apm.Logging;

namespace Elastic.Apm.DistributedTracing
{
	/// <summary>
	/// This is an implementation of the
	/// "https://www.w3.org/TR/trace-context/#traceparent-field" w3c 'traceparent' header draft.
	/// elastic-apm-traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
	/// (______________________)  () (______________________________) (______________) ()
	///      v                     v                 v                        v         v
	///  Header name           Version           Trace-Id                Span-Id     Flags
	/// Since the w3c document is just a draft at the moment,
	/// we don't use the official header name but prepend the custom prefix "Elastic-Apm-".
	/// </summary>
	internal struct TraceParent
	{
		private const int TraceParentLength = 55;
		private const byte FlagRecorded = 1; // 00000001

		internal const string TraceParentHeaderName = "elastic-apm-traceparent";

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

		public static (string parent, string traceId, bool isRecordedFalgActive) ParseTraceParentString(string traceParent)
		{
			var traceParentByte = traceParent.Substring(3, 32);
			var parentId = traceParent.Substring(36, 16);
			var isRecordedFlagActive = (Convert.ToByte(traceParent.Substring(53, 2)) & FlagRecorded) == FlagRecorded;

			return (traceParentByte, parentId, isRecordedFlagActive);
		}
	}
}
