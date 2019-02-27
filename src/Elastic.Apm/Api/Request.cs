using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Encapsulates Request related information that can be attached to an <see cref="ITransaction" /> through <see cref="ITransaction.Context" />
	/// See <see cref="Context.Request" />
	/// </summary>
	public struct Request
	{
		public Request(string method, Url url) => (Method, Url, Body, HttpVersion, Socket) = (method, url, null, null, new Socket());

		public string HttpVersion { get; set; }

		public string Method { get; set; }
		public Socket Socket { get; set; }
		public Url Url { get; set; }

		public object Body { get; set; }
	}

	public struct Socket
	{
		public bool Encrypted { get; set; }

		[JsonProperty("Remote_address")]
		public string RemoteAddress { get; set; }
	}

	public struct Url
	{
		public string Full { get; set; }
		public string HostName { get; set; }
		public string Protocol { get; set; }
		public string Raw { get; set; }
	}
}
