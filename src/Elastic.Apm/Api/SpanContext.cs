using System;
using System.Collections.Generic;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class SpanContext
	{
		private readonly Lazy<Dictionary<string, string>> _tags = new Lazy<Dictionary<string, string>>();
		public Database Db { get; set; }
		public Http Http { get; set; }

		[JsonConverter(typeof(TagsJsonConverter))]
		public Dictionary<string, string> Tags => _tags.Value;
	}
}
