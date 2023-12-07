// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Elastic.Transport;

namespace Elastic.Apm.Ingest;

/// <summary> </summary>
public class EventIntakeResponse : TransportResponse
{
	/// <summary> </summary>
	[JsonPropertyName("accepted")]
	public long Accepted { get; set; }

	/// <summary> </summary>
	[JsonPropertyName("errors")]
	//[JsonConverter(typeof(ResponseItemsConverter))]
	public IReadOnlyCollection<IntakeErrorItem> Errors { get; set; } = null!;
}

/// <summary> </summary>
public class IntakeErrorItem
{
	/// <summary> </summary>
	[JsonPropertyName("message")]
	public string Message { get; set; } = null!;

	/// <summary> </summary>
	[JsonPropertyName("document")]
	public string Document { get; set; } = null!;
}
