// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Report;

internal class IntakeResponse
{
	[JsonProperty("accepted")]
	public int Accepted { get; set; }

	[JsonProperty("errors")]
	public IReadOnlyCollection<IntakeError> Errors { get; set; }
}

internal class IntakeError
{

	[JsonProperty("message")]
	public string Message { get; set; }
}
