using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
	internal class Context
	{
		private readonly Lazy<Dictionary<string, string>> tags = new Lazy<Dictionary<string, string>>();
		public Request Request { get; set; }

		public Response Response { get; set; }

		public Dictionary<string, string> Tags => tags.Value;
	}
}
