using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Encapsulates Request related information that can be attached to an <see cref="ITransaction" /> through
	/// <see cref="ITransaction.Context" />
	/// See <see cref="Context.Request" />
	/// </summary>
	public class Request
	{
		public Request(string method, Url url) => (Method, Url) = (method, url);

		public object Body { get; set; }

		public Dictionary<string, string> Headers { get; set; }

		[JsonProperty("http_version")]
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string HttpVersion { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Method { get; set; }

		public Socket Socket { get; set; }

		public Url Url { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(Request)) { { "Method", Method }, { "Url", Url }, { "Socket", Socket } }.ToString();
	}

	public class Socket
	{
		public bool Encrypted { get; set; }

		[JsonProperty("remote_address")]
		public string RemoteAddress { get; set; }

		public override string ToString() =>
			new ToStringBuilder(nameof(Socket)) { { "Encrypted", Encrypted }, { "RemoteAddress", RemoteAddress } }.ToString();
	}

	public class Url
	{
		private string _full;
		private string _raw;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Full
		{
			get => _full;
			set => _full = Http.Sanitize(value, out var newValue) ? newValue : value;
		}

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("hostname")]
		public string HostName { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("pathname")]
		public string PathName { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Protocol { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Raw
		{
			get => _raw;
			set => _raw = Http.Sanitize(value, out var newValue) ? newValue : value;
		}

		/// <summary>
		/// The search describes the query string of the request.
		/// It is expected to have values delimited by ampersands.
		/// </summary>
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("search")]
		public string Search { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Url)) { { "Full", Full } }.ToString();
	}
}
