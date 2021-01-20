// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Elastic.Apm.Api;

namespace Elastic.Apm.DistributedTracing
{
	/// <summary>
	/// An implementation of the
	/// <a href="https://www.w3.org/TR/trace-context/#traceparent-field">w3c 'Trace Context' traceparent and tracestate</a>:
	///
	/// traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
	/// (_________)  () (______________________________) (______________) ()
	///      v       v                 v                        v         v
	/// Header name Version           Trace-Id                Span-Id     Flags
	/// </summary>
	internal static class TraceContext
	{
		private const byte FlagRecorded = 1; // 00000001
		private const int OptionsLength = 2;
		private const int SpanIdLength = 16;
		private const int TraceIdLength = 32;
		internal const string TraceParentHeaderName = "traceparent";
		internal const string TraceParentHeaderNamePrefixed = "elastic-apm-traceparent";
		internal const string TraceStateHeaderName = "tracestate";
		private const int VersionAndTraceIdAndSpanIdLength = 53;
		private const int VersionAndTraceIdLength = 36;
		private const int VersionPrefixIdLength = 3;

		/// <summary>
		/// Parses the traceparent and tracestate headers
		/// </summary>
		/// <param name="traceParentValue">The value of the traceparent header</param>
		/// <param name="traceStateValue">The value of the tracestate headers</param>
		/// <returns>The parsed data if parsing was successful, null otherwise.</returns>
		internal static DistributedTracingData TryExtractTracingData(string traceParentValue, string traceStateValue = null)
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

			if (traceStateValue is null)
				return new DistributedTracingData(traceId, parentId, (traceFlags & FlagRecorded) == FlagRecorded);

			TraceState traceState = null;
			var validatedTraceStateValue = TraceState.ValidateTracestate(traceStateValue);
			if (validatedTraceStateValue != null)
			{
				traceState = new TraceState();
				traceState.AddTextHeader(validatedTraceStateValue);
			}

			return new DistributedTracingData(traceId, parentId, (traceFlags & FlagRecorded) == FlagRecorded, traceState);
		}

		internal static bool IsHex(IEnumerable<char> chars)
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
			=> distributedTracingData == null
				? null
				: $"00-{distributedTracingData.TraceId}-{distributedTracingData.ParentId}-{(distributedTracingData.FlagRecorded ? "01" : "00")}";

		private static bool IsTraceIdValid(string traceId)
			=> !string.IsNullOrWhiteSpace(traceId) && traceId.Length == 32 && IsHex(traceId) && traceId != "00000000000000000000000000000000";

		private static bool IsTraceParentValid(string parentId)
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
