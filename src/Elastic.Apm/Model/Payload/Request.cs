﻿using System;

namespace Elastic.Apm.Model.Payload
{
	public class Request
	{
		public String HttpVersion { get; set; }

		public String Method { get; set; }
		public Socket Socket { get; set; }
		public Url Url { get; set; }
	}

	public class Socket
	{
		public bool Encrypted { get; set; }
		public String Remote_address { get; set; }
	}

	public class Url
	{
		public String Full { get; set; }
		public String HostName { get; set; }
		public String Protocol { get; set; }
		public String Raw { get; set; }
	}
}
