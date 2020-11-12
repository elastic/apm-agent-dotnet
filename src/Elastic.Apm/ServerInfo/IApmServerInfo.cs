// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.ServerInfo
{
	/// <summary>
	/// Encapsulates information about the APM Server that receives data from the agent.
	/// </summary>
	internal interface IApmServerInfo
	{
		/// <summary>
		/// The version of the APM server.
		/// This can be <code>null</code> if the agent has not yet queried the APM server for its version or the query failed.
		/// The agent should not depend on the APM server version and if the version is not (yet) available the agent should
		/// default to a reasonable behaviour.
		/// </summary>
		public ElasticVersion Version { get; set; }
	}
}
