using System;
using System.Collections.Generic;

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
	internal static class TraceParent
	{
		private const byte FlagRecorded = 1; // 00000001
		private const int OptionsLength = 2;
		private const int SpanIdLength = 16;
		private const int TraceIdLength = 32;
		internal const string TraceParentHeaderName = "elastic-apm-traceparent";
		private const int VersionAndTraceIdAndSpanIdLength = 53;
		private const int VersionAndTraceIdLength = 36;
		private const int VersionLength = 2;
		private const int VersionPrefixIdLength = 3;

		/// <summary>
		/// Checks if the <paramref name="traceFields" /> flag contains the <see cref="FlagRecorded" /> flag.
		/// </summary>
		/// <param name="traceFields">The traceOptions flags return from <see cref="TryExtractTraceparent" />.</param>
		/// <returns>true if <paramref name="traceFields" /> contains <see cref="FlagRecorded" />, false otherwise.</returns>
		public static bool IsFlagRecordedActive(byte traceFields) => (traceFields & FlagRecorded) == FlagRecorded;

		/// <summary>
		/// Parses the traceparent header
		/// </summary>
		/// <param name="traceParentValue">The value of the traceparent header</param>
		/// <param name="traceId">The parsed traceId</param>
		/// <param name="prentId">The parsed parentId</param>
		/// <param name="traceFields">The parsed traceOptions flags</param>
		/// <returns>True if parsing was successful, false otherwise.</returns>
		internal static bool TryExtractTraceparent(string traceParentValue, out string traceId, out string prentId, out byte traceFields)
		{
			traceId = string.Empty;
			prentId = string.Empty;
			traceFields = 0;

			var bestAttempt = false;

			if (string.IsNullOrWhiteSpace(traceParentValue)) return false;

			if (traceParentValue.Length < VersionPrefixIdLength || traceParentValue[VersionPrefixIdLength - 1] != '-') return false;

			try
			{
				var versionArray = StringToByteArray(traceParentValue, 0, VersionLength);
				if (versionArray[0] == 255)
					return false;

				if (versionArray[0] > 0)
					// expected version is 00
					// for higher versions - best attempt parsing of trace id, span id, etc.
					bestAttempt = true;
			}
			catch
			{
				return false;
			}

			if (traceParentValue.Length < VersionAndTraceIdLength || traceParentValue[VersionAndTraceIdLength - 1] != '-') return false;

			try
			{
				var traceIdVal =
					traceParentValue.Substring(VersionPrefixIdLength,
						TraceIdLength);

				if (!IsHex(traceIdVal) || traceIdVal == "00000000000000000000000000000000")
					return false;

				traceId = traceIdVal;
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}

			if (traceParentValue.Length < VersionAndTraceIdAndSpanIdLength
				|| traceParentValue[VersionAndTraceIdAndSpanIdLength - 1] != '-') return false;

			try
			{
				var prentIdVal = traceParentValue.Substring(VersionAndTraceIdLength, SpanIdLength);
				if (!IsHex(prentIdVal) || prentIdVal == "0000000000000000")
					return false;

				prentId = prentIdVal;
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}

			if (traceParentValue.Length < VersionAndTraceIdAndSpanIdLength + OptionsLength) return false;

			try
			{
				var fields = StringToByteArray(traceParentValue, VersionAndTraceIdAndSpanIdLength, OptionsLength);

				if (fields != null && fields.Length > 0)
					traceFields = fields[0];
			}
			catch (ArgumentOutOfRangeException)
			{
				return false;
			}

			if (!bestAttempt && traceParentValue.Length != VersionAndTraceIdAndSpanIdLength + OptionsLength) return false;

			// ReSharper disable once InvertIf - imo that would make it hard to read.
			if (bestAttempt)
			{
				if (traceParentValue.Length > VersionAndTraceIdAndSpanIdLength + OptionsLength &&
					traceParentValue[VersionAndTraceIdAndSpanIdLength + OptionsLength] != '-')
					return false;
			}

			bool IsHex(IEnumerable<char> chars)
			{
				// ReSharper disable once LoopCanBeConvertedToQuery - I benchmarked, that'd make parsing ~2x slower, don't do it!
				foreach (var c in chars)
				{
					var isHex = c >= '0' && c <= '9' ||
						c >= 'a' && c <= 'f' ||
						c >= 'A' && c <= 'F';

					if (!isHex)
						return false;
				}
				return true;
			}

			return true;
		}

		public static string BuildTraceparent(string traceId, string spanId)
			=> $"00-{traceId}-{spanId}-01";

		private static byte[] StringToByteArray(string src, int start = 0, int len = -1)
		{
			if (len == -1) len = src.Length;

			var size = len / 2;
			var bytes = new byte[size];
			for (int i = 0, j = start; i < size; i++)
			{
				var high = HexCharToInt(src[j++]);
				var low = HexCharToInt(src[j++]);
				bytes[i] = (byte)((high << 4) | low);
			}

			return bytes;

			int HexCharToInt(char c)
			{
				if (c >= '0' && c <= '9') return c - '0';

				if (c >= 'a' && c <= 'f') return c - 'a' + 10;

				if (c >= 'A' && c <= 'F') return c - 'A' + 10;

				throw new ArgumentOutOfRangeException("Invalid character: " + c);
			}
		}
	}
}
