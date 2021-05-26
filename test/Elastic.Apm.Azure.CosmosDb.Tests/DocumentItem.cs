// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Newtonsoft.Json;

namespace Elastic.Apm.Azure.CosmosDb.Tests
{
	public class DocumentItem
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		public string FirstName { get; set; }

		public string LastName { get; set; }

		public string PartitionKey => Id;
	}
}
