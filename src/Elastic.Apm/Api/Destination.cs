// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Public API to set properties that are stored under <c>span.destination</c> in
	/// <a href="https://www.elastic.co/guide/en/apm/get-started/current/transaction-spans.html">the APM data model</a>.
	/// </summary>
	public class Destination
	{
		private Optional<string> _address;
		private Optional<int?> _port;

		/// <summary>
		/// Either an IP (v4 or v6) or a host/domain name.
		/// See <a href="https://github.com/elastic/apm/issues/115#issuecomment-555814374">this issue</a> for more information.
		/// If this property is not set via this public API it will be deduced from other parts of <see cref="SpanContext"/>
		/// (for example <see cref="SpanContext.Http"/> or <see cref="SpanContext.Db"/>).
		/// Explicitly setting this property to <c>null</c> will prohibit this automatic deduction.
		/// </summary>
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Address
		{
			get => _address.Value;
			set => _address = new Optional<string>(value);
		}

		/// <summary>
		/// Port number - it should not be omitted even if it's the default port number for the corresponding protocol.
		/// See <a href="https://github.com/elastic/apm/issues/115#issuecomment-555814374">this issue</a> for more information.
		/// If this property is not set via this public API it will be deduced from other parts of <see cref="SpanContext"/>
		/// (for example <see cref="SpanContext.Http"/> or <see cref="SpanContext.Db"/>).
		/// Explicitly setting this property to <c>null</c> will prohibit this automatic deduction.
		/// </summary>
		public int? Port
		{
			get => _port.Value;
			set => _port = new Optional<int?>(value);
		}

		internal void CopyMissingPropertiesFrom(Destination src)
		{
			if (!_address.HasValue) _address = src._address;
			if (!_port.HasValue) _port = src._port;
		}

		internal bool AddressHasValue => _address.HasValue;

		/// <summary>
		/// The goal is to allow public API user to prohibit automatic deduction of any of  `context.destination` properties.
		/// To achieve that we need a way to distinguish between `null` as the initial value
		/// (meaning public API user is okay with us automatically deducing it) and `null` explicitly set via public API
		/// (meaning the user doesn't want us to automatically deduce it).
		/// </summary>
		private readonly struct Optional<T>
		{
			internal readonly bool HasValue;
			internal readonly T Value;

			internal Optional(T value)
			{
				Value = value;
				HasValue = true;
			}
		}
	}
}
