using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Request : IRequest
	{
		private string _httpVersion;
		private string _method;

		public string HttpVersion
		{
			get => _httpVersion ?? "";
			set => _httpVersion = value.TrimToMaxLength();
		}

		public object Body { get; set; }

		public string Method
		{
			get => _method ?? "";
			set => _method = value.TrimToMaxLength();
		}

		public Socket Socket { get; set; }
		public Url Url { get; set; }
	}

	internal class Socket
	{
		public bool Encrypted { get; set; }

		[JsonProperty("Remote_address")]
		public string RemoteAddress { get; set; }
	}

	internal class Url
	{
		private string _full;
		private string _raw;
		private string _hostName;
		private string _protocol;

		public string Full
		{
			get => _full ?? "";
			set => _full = value.TrimToMaxLength();
		}

		public string HostName
		{
			get => _hostName ?? "";
			set => _hostName = value.TrimToMaxLength();
		}

		public string Protocol
		{
			get => _protocol ?? "";
			set => _protocol = value.TrimToMaxLength();
		}


		public string Raw
		{
			get => _raw ?? "";
			set => _raw = value.TrimToMaxLength();
		}
	}
}
