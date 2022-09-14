// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Data related to the cloud or infrastructure the events are coming from.
	/// </summary>
	public class CloudContext
	{
		public CloudOrigin Origin { get; set; }
	}

	public struct CloudOrigin
	{
		/// <summary>
		/// The cloud account or organization id used to identify different entities in a multi-tenant environment.
		/// </summary>
		public string Account { get; set; }

		/// <summary>
		/// The cloud account or organization id used to identify different entities in a multi-tenant environment.
		/// </summary>
		public string Id { get; set; }
	}
}
