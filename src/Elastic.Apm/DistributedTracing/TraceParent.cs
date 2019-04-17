using System;
using System.Collections.Generic;
using Elastic.Apm.Api;

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
		private const int VersionPrefixIdLength = 3;

		/// <summary>
		/// Parses the traceparent header
		/// </summary>
		/// <param name="traceParentValue">The value of the traceparent header</param>
		/// <returns>The parsed data if parsing was successful, null otherwise.</returns>
		internal static DistributedTracingData TryExtractTraceparent(string traceParentValue)
		{
			var bestAttempt = false;

			if (string.IsNullOrWhiteSpace(traceParentValue)) return null;

			if (traceParentValue.Length < VersionPrefixIdLength || traceParentValue[VersionPrefixIdLength - 1] != '-') return null;

			try
			{
				var versionArray = HexStringTwoCharToByte(traceParentValue);
				if (versionArray == 255)
					return null;

				if (versionArray > 0)
					// expected version is 00
					// for higher versions - best attempt parsing of trace id, span id, etc.
					bestAttempt = true;
			}
			catch
			{
				return null;
			}

			if (traceParentValue.Length < VersionAndTraceIdLength || traceParentValue[VersionAndTraceIdLength - 1] != '-') return null;

			string traceId;
			try
			{
				var traceIdVal =
					traceParentValue.Substring(VersionPrefixIdLength,
						TraceIdLength);

				if (!IsTraceIdValid(traceIdVal))
					return null;

				traceId = traceIdVal;
			}
			catch (ArgumentOutOfRangeException)
			{
				return null;
			}

			if (traceParentValue.Length < VersionAndTraceIdAndSpanIdLength
				|| traceParentValue[VersionAndTraceIdAndSpanIdLength - 1] != '-') return null;

			string parentId;
			try
			{
				var parentIdVal = traceParentValue.Substring(VersionAndTraceIdLength, SpanIdLength);

				if (!IsTraceParentValid(parentIdVal))
					return null;

				parentId = parentIdVal;
			}
			catch (ArgumentOutOfRangeException)
			{
				return null;
			}

			if (traceParentValue.Length < VersionAndTraceIdAndSpanIdLength + OptionsLength) return null;

			byte traceFlags;
			try
			{
				traceFlags = HexStringTwoCharToByte(traceParentValue, VersionAndTraceIdAndSpanIdLength);
			}
			catch (ArgumentOutOfRangeException)
			{
				return null;
			}

			if (!bestAttempt && traceParentValue.Length != VersionAndTraceIdAndSpanIdLength + OptionsLength) return null;

			// ReSharper disable once InvertIf - imo that would make it hard to read.
			if (bestAttempt)
			{
				if (traceParentValue.Length > VersionAndTraceIdAndSpanIdLength + OptionsLength &&
					traceParentValue[VersionAndTraceIdAndSpanIdLength + OptionsLength] != '-')
					return null;
			}

			return new DistributedTracingData(traceId, parentId, (traceFlags & FlagRecorded) == FlagRecorded);
		}

		private static bool IsHex(IEnumerable<char> chars)
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

		public static string BuildTraceparent(DistributedTracingData distributedTracingData)
			=> $"00-{distributedTracingData.TraceId}-{distributedTracingData.ParentId}-" + (distributedTracingData.FlagRecorded ? "01" : "00");

		internal static bool IsTraceIdValid(string traceId)
			=> !string.IsNullOrWhiteSpace(traceId) && traceId.Length == 32 && IsHex(traceId) && traceId != "00000000000000000000000000000000";

		internal static bool IsTraceParentValid(string parentId)
			=> !string.IsNullOrWhiteSpace(parentId) && parentId.Length == 16 && IsHex(parentId) && parentId != "0000000000000000";

		/// <summary>
		/// Converts 2 selected chars from the input string into a byte
		/// </summary>
		/// <param name="src">The string to convert - must be at least 2 char long</param>
		/// <param name="start">The position of the first character to convert.</param>
		/// <returns>The byte representation of the string</returns>
		private static byte HexStringTwoCharToByte(string src, int start = 0)
		{
			if (string.IsNullOrWhiteSpace(src) || src.Length <= start + 1)
				throw new Exception("String is expected to be at least 2 char long");

			var high = HexCharToInt(src[start]);
			var low = HexCharToInt(src[start + 1]);
			var retVal = (byte)((high << 4) | low);

			return retVal;

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
