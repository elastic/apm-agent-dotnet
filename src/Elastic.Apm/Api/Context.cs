using System;
using System.Collections.Generic;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Context
	{
		private readonly Lazy<Dictionary<string, string>> _tags = new Lazy<Dictionary<string, string>>();

		/// <summary>
		/// If a log record was generated as a result of a http request, the http interface can be used to collect this
		/// information.
		/// This property is by default null! You have to assign a <see cref="Request" /> instance to this property in order to use
		/// it.
		/// </summary>
		public Request Request { get; set; }

		/// <summary>
		/// If a log record was generated as a result of a http request, the http interface can be used to collect this
		/// information.
		/// This property is by default null! You have to assign a <see cref="Response" /> instance to this property in order to
		/// use
		/// it.
		/// </summary>
		public Response Response { get; set; }

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

		public User User { get; set; }
	}
}
