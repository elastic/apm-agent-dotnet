using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
	internal class Context
	{
		private readonly Lazy<Dictionary<string, string>> _tags = new Lazy<Dictionary<string, string>>();
		public Lazy<User> User { get; set; }
		public Lazy<Request> Request { get; set; }

		public Lazy<Response> Response { get; set; }

		public Dictionary<string, string> Tags => _tags.Value;
	}
}
