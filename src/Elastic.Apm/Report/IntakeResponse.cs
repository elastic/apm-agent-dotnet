// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Elastic.Apm.Report;

internal class IntakeResponse
{
	[JsonPropertyName("accepted")]
	public int Accepted { get; set; }

	[JsonPropertyName("errors")]
	public IReadOnlyCollection<IntakeError> Errors { get; set; }
}

internal class IntakeError
{

	[JsonPropertyName("message")]
	public string Message { get; set; }
}
