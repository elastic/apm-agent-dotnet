// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Elastic.Apm.Azure.Storage.Tests
{
	public class BlobContainerScope : IAsyncDisposable
	{
		public string ContainerName { get; }
		private readonly BlobContainerInfo _properties;
		public BlobContainerClient ContainerClient { get; }

		private BlobContainerScope(BlobContainerClient adminClient, string containerName, BlobContainerInfo properties)
		{
			ContainerClient = adminClient;
			ContainerName = containerName;
			_properties = properties;
		}

		public static async Task<BlobContainerScope> CreateContainer(string connectionString)
		{
			var containerName = Guid.NewGuid().ToString("D");
			var containerClient = new BlobContainerClient(connectionString, containerName);
			var response = await containerClient.CreateAsync().ConfigureAwait(false);
			return new BlobContainerScope(containerClient, containerName, response.Value);
		}

		public async ValueTask DisposeAsync() =>
			await ContainerClient.DeleteAsync().ConfigureAwait(false);
	}
}
