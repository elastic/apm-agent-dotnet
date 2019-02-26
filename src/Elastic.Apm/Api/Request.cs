namespace Elastic.Apm.Api
{
	/// <summary>
	/// Encapsulates Request related information that can be attached to an <see cref="ITransaction" />.
	/// See <see cref="ITransaction.Request" />
	/// </summary>
	public class Request
	{
		public Request(string method, string protocol) => (Method, UrlProtocol) = (method, protocol);

		public string HttpVersion { get; set; }
		public string Method { get; }

		public bool SocketEncrypted { get; set; }
		public string SocketRemoteAddress { get; set; }

		public string UrlFull { get; set; }
		public string UrlHostName { get; set; }
		public string UrlProtocol { get; }
		public string UrlRaw { get; set; }
	}
}
