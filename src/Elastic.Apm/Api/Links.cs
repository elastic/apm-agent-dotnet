// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Apm.Api.Constraints;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Represents a span link.
	/// Links holds links to other spans, potentially in other traces.
	/// </summary>
	public class SpanLink
	{
		public SpanLink(string spanId, string traceId)
		{
			SpanId = spanId;
			TraceId = traceId;
		}

		[JsonPropertyName("span_id")]
		[MaxLength]
		public string SpanId { get; }

		[JsonPropertyName("trace_id")]
		[MaxLength]
		public string TraceId { get; }
	}
}
