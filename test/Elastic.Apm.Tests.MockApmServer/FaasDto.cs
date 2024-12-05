// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information


using System.Text.Json.Serialization;

namespace Elastic.Apm.Tests.MockApmServer;

public class FaasDto
{
	[JsonPropertyName("cold_start")]
	public bool ColdStart { get; set; }

	public string Execution { get; set; }

	public string Id { get; set; }

	public string Name { get; set; }

	public string Version { get; set; }

	public TriggerDto Trigger { get; set; }

	public override string ToString() => $"{Name} ({Id})";
}

public class TriggerDto
{
	[JsonPropertyName("request_id")] public string RequestId { get; set; }

	public string Type { get; set; }

	public override string ToString() => Type;
}
