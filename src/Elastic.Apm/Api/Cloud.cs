// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api.Constraints;
using Newtonsoft.Json;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Fields related to the cloud or infrastructure the events are coming from.
	/// </summary>
	// TODO: Add Spec attribute once https://github.com/elastic/apm-agent-dotnet/pull/984 is merged.
	public class Cloud
	{
		public CloudAccount Account { get; set; }

		public CloudInstance Instance { get; set; }

		[MaxLength]
		[JsonProperty("availability_zone")]
		public string AvailabilityZone { get; set; }

		public CloudMachine Machine { get; set; }

		/// <summary>
		/// The cloud provider, for example, aws, gcp, azure.
		/// </summary>
		[MaxLength]
		[Required]
		public string Provider { get; set; }

		[MaxLength]
		public string Region { get; set; }
		public CloudProject Project { get; set; }
	}

	public class CloudProject
	{
		/// <summary>
		/// Cloud project name
		/// </summary>
		[MaxLength]
		public string Name { get; set; }

		/// <summary>
		/// Cloud project id
		/// </summary>
		[MaxLength]
		public string Id { get; set; }
	}

	/// <summary>
	/// An instance in a cloud provider
	/// </summary>
	public class CloudInstance
	{
		/// <summary>
		/// Instance ID of the host machine.
		/// </summary>
		[MaxLength]
		public string Id { get; set; }

		/// <summary>
		/// Instance name of the host machine.
		/// </summary>
		[MaxLength]
		public string Name { get; set; }
	}

	public class CloudAccount
	{
		/// <summary>
		/// The cloud account or organization id used to identify different entities in a multi-tenant environment.
		/// <para/>
		/// <para/>
		/// Examples: AWS account id, Google Cloud ORG Id, or other unique identifier.
		/// </summary>
		[MaxLength]
		public string Id { get; set; }

		/// <summary>
		/// The cloud account name
		/// </summary>
		[MaxLength]
		public string Name { get; set; }
	}

	public class CloudMachine
	{
		/// <summary>
		/// Machine type of the host machine.
		/// </summary>
		[MaxLength]
		public string Type { get; set; }
	}
}
