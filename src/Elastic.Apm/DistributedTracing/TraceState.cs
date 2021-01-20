// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Elastic.Apm.DistributedTracing
{
	/// <summary>
	/// Handles <see cref="TraceContext"/> tracestate
	/// </summary>
	internal class TraceState
	{
		private const int DefaultSizeLimit = 4096;
		private const char VendorSeparator = ',';
		private const char EntrySeparator = ';';
		private const string VendorPrefix = "es=";
		private const string SampleRatePrefix = "s:";
		private const string FullPrefix = VendorPrefix + SampleRatePrefix;

		public double? SampleRate { get; private set; }

		private List<string> _tracestate;
		private StringBuilder _rewriteBuffer;
		private int _sizeLimit;

		public int SizeLimit
		{
			get => _sizeLimit;
			set
			{
				if (_tracestate.Count > 0)
					throw new InvalidOperationException("can't change size limit once headers have been added");

				_sizeLimit = value;
			}
		}

		public TraceState(double sampleRate) : this() => SetSampleRate(sampleRate);

		public TraceState()
		{
			_tracestate = new List<string>();
			_sizeLimit = DefaultSizeLimit;
		}

		/// <summary>
		/// Sets the sample rate
		/// </summary>
		/// <param name="sampleRate">The sample rate</param>
		/// <exception cref="InvalidOperationException">
		/// thrown if the sample rate has already been set
		/// </exception>
		internal void SetSampleRate(double sampleRate)
		{
			if (SampleRate.HasValue)
			{
				// sample rate is set either explicitly from this method (for root transactions)
				// or through upstream header, thus there is no need to change after. This allows to only
				// write/rewrite headers once
				throw new InvalidOperationException("sample rate has already been set from headers");
			}

			SampleRate = sampleRate;
			_tracestate.Add(GetHeaderValue(sampleRate));
		}

		/// <summary>
		/// Adds a text header to tracestate
		/// </summary>
		/// <param name="headerValue">The header value</param>
		public void AddTextHeader(string headerValue)
		{
			var elasticVendorIndex = headerValue.IndexOf(VendorPrefix, StringComparison.Ordinal);

			if (elasticVendorIndex != -1)
			{
				var entriesStart = headerValue.IndexOf(SampleRatePrefix, elasticVendorIndex, StringComparison.Ordinal);
				if (entriesStart != -1)
				{
					var valueStart = entriesStart + SampleRatePrefix.Length;
					var valueEnd = valueStart;
					if (valueEnd < headerValue.Length)
					{
						var c = headerValue[valueEnd];
						while (valueEnd < headerValue.Length && c != VendorSeparator && c != EntrySeparator)
							c = headerValue[valueEnd++];

						if (valueEnd < headerValue.Length)
						{
							// end due to separator char that needs to be trimmed
							valueEnd--;
						}
					}

					if (double.TryParse(headerValue.Substring(valueStart, valueEnd - valueStart), out var value))
					{
						if (value >= 0 && value <= 1)
						{
							// ensure proper rounding of sample rate to minimize storage
							// even if configuration should not allow this, any upstream value might require rounding
							var rounded = Sampler.RoundToPrecision(value);

							// ReSharper disable once CompareOfFloatsByEqualityOperator
							if (rounded != value)
							{
								if (_rewriteBuffer is null)
									_rewriteBuffer = new StringBuilder();
								else
									_rewriteBuffer.Clear();

								_rewriteBuffer.Append(headerValue, 0, valueStart);
								_rewriteBuffer.Append(rounded);
								_rewriteBuffer.Append(headerValue, valueEnd, headerValue.Length - valueEnd);
								headerValue = _rewriteBuffer.ToString();
							}
							SampleRate = rounded;
						}
					}
				}
			}

			_tracestate.Add(headerValue);
		}

		/// <summary>
		/// Validates the tracestate value
		/// </summary>
		/// <param name="traceState">The value to validate</param>
		/// <returns>The <paramref name="traceState"/> if the value is a valid trace state, <code>null</code> otherwise</returns>
		public static string ValidateTracestate(string traceState)
		{
			if (string.IsNullOrEmpty(traceState))
				return null;

			var listMembers = traceState.Split(VendorSeparator);
			var set = new HashSet<string>();

			if (listMembers.Length == 0 || listMembers.Length > 32) return null;

			var sb = new StringBuilder();

			foreach (var listMember in listMembers)
			{
				// TODO: Span-ify
				var item = listMember.Split('=');
				if (item.Length != 2)
					continue;

				if (set.Contains(item[0]))
					return null;

				if (item[0].Length > 256)
					return null;

				if (item[0].Contains('@'))
				{
					var vendorFormatKey = item[0].Split('@');
					if (vendorFormatKey.Length != 2)
						return null;

					if (vendorFormatKey[0].Length == 0 || string.IsNullOrEmpty(vendorFormatKey[0]) || vendorFormatKey[0].Length > 241)
						return null;
					if (vendorFormatKey[1].Length == 0 || string.IsNullOrEmpty(vendorFormatKey[1]) || vendorFormatKey[1].Length > 14)
						return null;

					if (!IsValidKey(vendorFormatKey[0]) || !IsValidKey(vendorFormatKey[1])) return null;
				}
				else
				{
					if (!IsValidKey(item[0]))
						return null;
				}

				if (!IsValidValue(item[1]))
					return null;

				if (sb.Length != 0)
					sb.Append(VendorSeparator);

				sb.Append(listMember);
				set.Add(item[0]);
			}

			return sb.Length != traceState.Length ? sb.ToString() : traceState;
		}

		private static bool IsValidKey(string str)
		{
			// ReSharper disable once LoopCanBeConvertedToQuery
			for (var i = 0; i < str.Length; i++)
			{
				var c = str[i];
				var isOk = c >= '0' && c <= '9' || c >= 'a' && c <= 'z' || c == '_' || c == '-' || c == '*' || c == '/' || c == '\t'
					//OWS rule: if we hit a ' ', then it must be next to a '\t'
					|| c == ' ' && (i > 0 && str[i - 1] == '\t' || i < str.Length - 2 && str[i + 1] == '\t');

				if (!isOk) return false;
			}

			return true;
		}

		private static bool IsValidValue(string str)
		{
			if (string.IsNullOrEmpty(str)) return false;

			// ReSharper disable once LoopCanBeConvertedToQuery
			for (var i = 0; i < str.Length; i++)
			{
				var c = str[i];
				var isOk = c >= 0x20 && c <= 0x7E || c == '\t' && c != ',' && c != '='
					//OWS rule: if we hit a ' ', then it must be next to a '\t'
					|| c == ' ' && (i > 0 && str[i - 1] == '\t' || i < str.Length - 2 && str[i + 1] == '\t');

				if (!isOk) return false;
			}

			return true;
		}

		/// <summary>
		/// Gets the tracestate header value for the agent
		/// </summary>
		/// <param name="sampleRate">The sample rate</param>
		/// <returns>The tracestate header</returns>
		public static string GetHeaderValue(double sampleRate) => FullPrefix + sampleRate;

		/// <summary>
		/// Creates the tracestate text header to send in outgoing requests
		/// </summary>
		/// <returns>The tracestate text header</returns>
		public string ToTextHeader() => _tracestate.Count == 0
			? null
			: TracestateBuilder.Instance.Build(_tracestate, SizeLimit);

		/// <summary>
		/// Per thread <see cref="StringBuilder"/> used to concatenate tracestate header.
		/// </summary>
		private class TracestateBuilder
		{
			internal static readonly TracestateBuilder Instance = new TracestateBuilder();

			private readonly ThreadLocal<StringBuilder> _builder = new ThreadLocal<StringBuilder>();

			public string Build(List<string> tracestate, int sizeLimit)
			{
				var singleTracestate = tracestate.Count != 1 ? null : tracestate[0];
				if (singleTracestate != null && singleTracestate.Length <= sizeLimit)
					return singleTracestate;

				var builder = GetStringBuilder();
				for (int i = 0, size = tracestate.Count; i < size; i++)
				{
					var value = tracestate[i];
					// ignore null entries to allow removing entries without resizing collection
					if (value != null)
						AppendTracestateHeaderValue(value, builder, sizeLimit);
				}

				return builder.Length == 0 ? null : builder.ToString();
			}

			private static void AppendTracestateHeaderValue(string headerValue, StringBuilder builder, int sizeLimit)
			{
				var requiredLength = headerValue.Length;
				var needsComma = builder.Length > 0;
				if (needsComma)
					requiredLength++;

				if (builder.Length + requiredLength <= sizeLimit)
				{
					// header fits completely
					if (needsComma)
						builder.Append(VendorSeparator);

					builder.Append(headerValue);
				}
				else
				{
					// only part of header might be included
					// When trimming due to size limit, we must include complete entries
					var endIndex = 0;
					for (var i = headerValue.Length - 1; i >= 0; i--)
					{
						if (headerValue[i] == VendorSeparator && builder.Length + i < sizeLimit)
						{
							endIndex = i;
							break;
						}
					}

					if (endIndex > 0)
					{
						if (builder.Length > 0)
							builder.Append(VendorSeparator);

						builder.Append(headerValue, 0, endIndex);
					}
				}
			}

			private TracestateBuilder() { }

			private StringBuilder GetStringBuilder()
			{
				StringBuilder builder;
				if (_builder.IsValueCreated)
				{
					builder = _builder.Value;
					builder.Clear();
				}
				else
				{
					builder = new StringBuilder();
					_builder.Value = builder;
				}

				return builder;
			}
		}
	}
}
