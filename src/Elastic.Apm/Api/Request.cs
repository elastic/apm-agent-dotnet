using System.Collections.Generic;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Encapsulates Request related information that can be attached to an <see cref="ITransaction" /> through <see cref="ITransaction.Context" />
	/// See <see cref="Context.Request" />
	/// </summary>
	public class Request
	{
		public Request(string method, Url url) => (Method, Url) = (method, url);

		[JsonProperty("http_version")]
		public string HttpVersion { get; set; }

		public string Method { get; set; }
		public Socket Socket { get; set; }
		public Url Url { get; set; }

		public object Body { get; set; }
		[JsonProperty("headers")]
		public Dictionary<string, object> Headers { get; set; }
	}

	public class Socket
	{
		public bool Encrypted { get; set; }

		[JsonProperty("remote_address")]
		public string RemoteAddress { get; set; }
	}

	public class Url
	{
		public string Full { get; set; }
		public string HostName { get; set; }
		public string Protocol { get; set; }
		public string Raw { get; set; }
	}
}
