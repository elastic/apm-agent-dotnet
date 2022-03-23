// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// Represents a span link.
	/// Links holds links to other spans, potentially in other traces.
	/// </summary>
	internal class Link
	{
		[JsonProperty("span_id")]
		[MaxLength]
		public string SpanId { get; set; }

		[JsonProperty("trace_id")]
		[MaxLength]
		public string TraceId { get; set; }

	}
}
