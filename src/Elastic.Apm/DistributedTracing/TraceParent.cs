using System;
using System.Collections.Generic;
using System.Text;

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
		private const int VersionPrefixIdLength = 3;
		private const int VersionLength = 2;
		private const int VersionAndTraceIdLength = 36;

		private const int TraceIdLength = 32;
		private const int VersionAndTraceIdAndSpanIdLength = 53;
		private const int SpanIdLength = 16;
		private const int OptionsLength = 2;

		private const byte FlagRecorded = 1; // 00000001
		internal const string TraceParentHeaderName = "elastic-apm-traceparent";

		/// <summary>
		/// Checks if the <paramref name="traceOptions" /> flag contains the <see cref="FlagRecorded" /> flag.
		/// </summary>
		/// <param name="traceOptions">The traceOptions flags return from <see cref="TryExtractTraceparent" />.</param>
		/// <returns>true if <paramref name="traceOptions" /> contains <see cref="FlagRecorded" />, false otherwise.</returns>
		public static bool IsFlagRecordedActive(byte[] traceOptions) => (traceOptions[0] & FlagRecorded) == FlagRecorded;

		/// <summary>
		/// Parses the traceparent header
		/// </summary>
		/// <param name="traceParentValue">The value of the traceparent header</param>
		/// <param name="traceId">The parsed traceId</param>
		/// <param name="prentId">The parsed parentId</param>
		/// <param name="traceOptions">The parsed traceOptions flags</param>
		/// <returns>True if parsing was successful, false otherwise.</returns>
		internal static bool TryExtractTraceparent(string traceParentValue, out string traceId, out string prentId, out byte[] traceOptions)
		{
			traceId = string.Empty;
			prentId = string.Empty;
			traceOptions = null;

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
				//parse value to byte array and back to string:
				//TODO: probably we should just validate the chars
				var traceIdVal = ByteArrayToString(StringToByteArray(traceParentValue, VersionPrefixIdLength, TraceIdLength));

				if (traceIdVal == "00000000000000000000000000000000")
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
				//parse value to byte array and back to string:
				//TODO: probably we should just validate the chars
				var prentIdVal = ByteArrayToString(StringToByteArray(traceParentValue, VersionAndTraceIdLength, SpanIdLength));

				if (prentIdVal == "0000000000000000")
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
				traceOptions = StringToByteArray(traceParentValue, VersionAndTraceIdAndSpanIdLength, OptionsLength);
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

			return true;
		}

		private static string ByteArrayToString(IEnumerable<byte> bytes)
		{
			var sb = new StringBuilder();
			foreach (var t in bytes) sb.Append(ByteToHexCharArray(t));

			return sb.ToString();

			char[] ByteToHexCharArray(byte b)
			{
				var result = new char[2];

				result[0] = (char)ByteToHexLookupTable[b];
				result[1] = (char)(ByteToHexLookupTable[b] >> 16);

				return result;
			}
		}

		private static readonly uint[] ByteToHexLookupTable = CreateLookupTable();

		// https://stackoverflow.com/a/24343727
		private static uint[] CreateLookupTable()
		{
			var table = new uint[256];
			for (var i = 0; i < 256; i++)
			{
				var s = i.ToString("x2");
				table[i] = s[0];
				table[i] += (uint)s[1] << 16;
			}

			return table;
		}

		public static string GetTraceParentVal(string traceId, string spanId)
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
