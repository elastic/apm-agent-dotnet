// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	public class Node
	{
		[JsonProperty("configured_name")]
		[MaxLength]
		public string ConfiguredName { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Node)) { { nameof(ConfiguredName), ConfiguredName } }.ToString();
	}
}
