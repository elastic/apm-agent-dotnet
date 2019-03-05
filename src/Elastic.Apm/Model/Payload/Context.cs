using System;
using System.Collections.Generic;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Context
	{
		private readonly Lazy<Dictionary<string, string>> tags = new Lazy<Dictionary<string, string>>();
		public Request Request { get; set; }

		public Response Response { get; set; }

		[JsonConverter(typeof(TagsJsonConverter))]
		public Dictionary<string, string> Tags => tags.Value;
	}
}
