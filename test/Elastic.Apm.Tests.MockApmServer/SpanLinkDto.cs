// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Tests.MockApmServer;

internal class SpanLinkDto
{
	[JsonProperty("span_id")]
	public string SpanId { get; set; }

	[JsonProperty("trace_id")]
	public string TraceId { get; set; }
}
