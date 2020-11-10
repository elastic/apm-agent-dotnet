// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;

namespace Elastic.Apm.Cloud
{
	/// <summary>
	/// Provides metadata for a cloud provider
	/// </summary>
	public interface ICloudMetadataProvider
	{
		/// <summary>
		/// The name of the cloud provider
		/// </summary>
		string Provider { get; }

		/// <summary>
		/// Retrieves the cloud metadata for the provider
		/// </summary>
		/// <returns></returns>
		Task<Api.Cloud> GetMetadataAsync();
	}
}
