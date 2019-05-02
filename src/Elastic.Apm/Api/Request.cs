using System.Collections.Generic;
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
		public string HttpVersion { get; set; }

		public string Method { get; set; }
		public Socket Socket { get; set; }
		public Url Url { get; set; }
	}

	public class Socket
	{
		public bool Encrypted { get; set; }

		[JsonProperty("remote_address")]
		public string RemoteAddress { get; set; }
	}

	public class Url
	{
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Full { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string HostName { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("pathname")]
		public string PathName { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Protocol { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Raw { get; set; }

		/// <summary>
		/// The search describes the query string of the request.
		/// It is expected to have values delimited by ampersands.
		/// </summary>
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("search")]
		public string Search { get; set; }
	}
}
