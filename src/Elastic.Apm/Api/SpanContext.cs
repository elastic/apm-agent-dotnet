using System;
using System.Collections.Generic;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class SpanContext
	{
		private readonly Lazy<Dictionary<string, string>> _labels = new Lazy<Dictionary<string, string>>();
		public Database Db { get; set; }
		public Http Http { get; set; }

		/// <summary>
		/// <seealso cref="ShouldSerializeLabels" />
		/// </summary>
		[JsonProperty("tags")]
		[JsonConverter(typeof(LabelsJsonConverter))]
		public Dictionary<string, string> Labels => _labels.Value;

		/// <summary>
		/// Method to conditionally serialize <see cref="Labels" /> - serialize only when there is at least one label.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeLabels() => _labels.IsValueCreated && Labels.Count > 0;
	}
}
