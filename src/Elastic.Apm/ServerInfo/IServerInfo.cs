// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;

namespace Elastic.Apm.ServerInfo
{
	/// <summary>
	/// Encapsulates information about the APM Server that receives data from the agent.
	/// </summary>
	internal interface IServerInfo
	{
		/// <summary>
		/// Indicates if the Server Info Endpoint whether queried.
		/// </summary>
		public bool ServerVersionQueried { get; }

		/// <summary>
		/// The version of the server.
		/// Only the following 3 fields are filled:
		/// <see cref="System.Version.Major" />
		/// <see cref="System.Version.Minor" />
		/// <see cref="System.Version.Build" />
		/// </summary>
		public Version Version { get; }

		/// <summary>
		/// Queries the Server Info Endpoint.
		/// </summary>
		/// <returns></returns>
		public Task GetServerInfoAsync();
	}
}
