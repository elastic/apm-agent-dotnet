using System;
using System.Collections.Generic;

namespace Elastic.Apm.Model.Payload
{
	internal class Context
	{
		public Request Request { get; set; }

		public Response Response { get; set; }

		private readonly Lazy<Dictionary<string, string>> tags = new Lazy<Dictionary<string, string>>();

		public Dictionary<string, string> Tags => tags.Value;
	}
}
