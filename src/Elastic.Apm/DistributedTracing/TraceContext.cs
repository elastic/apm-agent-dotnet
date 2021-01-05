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
	/// This is an implementation of the
	/// "https://www.w3.org/TR/trace-context/#traceparent-field" w3c 'Trace Context'.
	/// traceparent header:
	/// traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01
	/// (_________)  () (______________________________) (______________) ()
	///      v                     v                 v                        v         v
	///  Header name           Version           Trace-Id                Span-Id     Flags
	/// Also handles the tracestate header.
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
		/// Parses the traceparent header
		/// </summary>
		/// <param name="traceParentValue">The value of the traceparent header</param>
		/// <param name="traceStateValue">Tge value of the tracestate header</param>
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

			return traceStateValue != null
				? new DistributedTracingData(traceId, parentId, (traceFlags & FlagRecorded) == FlagRecorded, ValidateTracestate(traceStateValue))
				: new DistributedTracingData(traceId, parentId, (traceFlags & FlagRecorded) == FlagRecorded);
		}

		/// <summary>
		/// Validates the tracestate value
		/// </summary>
		/// <param name="traceState">The value to validate</param>
		/// <returns>The <paramref name="traceState"/> if the value is a valid trace state, <code>null</code> otherwise</returns>
		private static string ValidateTracestate(string traceState)
		{
			if (string.IsNullOrEmpty(traceState))
				return null;

			var listMembers = traceState.Split(',');
			var set = new HashSet<string>();

			if (!listMembers.Any() || listMembers.Length > 32) return null;

			var sb = new StringBuilder();

			foreach (var listMember in listMembers)
			{
				var item = listMember.Split('=');
				if (item.Count() != 2)
					continue;

				if (set.Contains(item[0]))
					return null;

				if (item[0].Length > 256)
					return null;

				if (item[0].Contains('@'))
				{
					var vendorFormatKey = item[0].Split('@');
					if (vendorFormatKey.Count() != 2)
						return null;

					if (vendorFormatKey[0].Length == 0 || string.IsNullOrEmpty(vendorFormatKey[0]) || vendorFormatKey[0].Length > 241)
						return null;
					if (vendorFormatKey[1].Length == 0 || string.IsNullOrEmpty(vendorFormatKey[1]) || vendorFormatKey[1].Length > 14)
						return null;

					if (!ValidateKey(vendorFormatKey[0]) || !ValidateKey(vendorFormatKey[1])) return null;
				}
				else
				{
					if (!ValidateKey(item[0]))
						return null;
				}

				if (!ValidateValue(item[1]))
					return null;

				if (sb.Length != 0)
					sb.Append(',');
				sb.Append(listMember);

				set.Add(item[0]);
			}

			return sb.Length != traceState.Length ? sb.ToString() : traceState;

			static bool ValidateValue(string str)
			{
				if (string.IsNullOrEmpty(str))
					return false;

				// ReSharper disable once LoopCanBeConvertedToQuery
				for (var i = 0; i < str.Length; i++)
				{
					var c = str[i];
					var isOk = c >= 0x20 && c <= 0x7E || c == '\t' && c != ',' && c != '='
						//OWS rule: if we hit a ' ', then it must be next to a '\t'
						|| c == ' ' && (i > 0 && str[i - 1] == '\t' || i < str.Length - 2 && str[i + 1] == '\t');

					if (!isOk)
						return false;
				}

				return true;
			}

			static bool ValidateKey(string str)
			{
				// ReSharper disable once LoopCanBeConvertedToQuery
				for (var i = 0; i < str.Length; i++)
				{
					var c = str[i];
					var isOk = c >= '0' && c <= '9' ||
						c >= 'a' && c <= 'z'
						|| c == '_' || c == '-' || c == '*' || c == '/' || c == '\t'
						//OWS rule: if we hit a ' ', then it must be next to a '\t'
						|| c == ' ' && (i > 0 && str[i - 1] == '\t' || i < str.Length - 2 && str[i + 1] == '\t');

					if (!isOk)
						return false;
				}

				return true;
			}
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

		internal static string BuildTraceState(DistributedTracingData distributedTracingData)
			=> distributedTracingData.TraceState;
	}
}
