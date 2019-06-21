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

		/// <summary>
		/// <seealso cref="ShouldSerializeTags" />
		/// </summary>
		[JsonConverter(typeof(TagsJsonConverter))]
		public Dictionary<string, string> Tags => _tags.Value;

		/// <summary>
		/// Method to conditionally serialize <see cref="Tags" /> - serialize only when there is at least one tag.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeTags() => _tags.IsValueCreated && Tags.Count > 0;
	}
}
