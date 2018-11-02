using System;
using System.Collections.Generic;
using System.Text;

namespace Elastic.Agent.Core.Model.Payload
{
    class Request
    {
		public String HttpVersion { get; set; }
		public Socket Socket { get; set; }
		public Url Url { get; set; }

		public String Method { get; set; }
	}

	class Socket
	{
		public bool Encrypted { get; set; }
		public String Remote_address { get; set; }
	}

	class Url
	{
		public String Raw { get; set; }
		public String Protocol { get; set; }
		public String Full { get; set; }
		public String HostName { get; set; }
	}
}
