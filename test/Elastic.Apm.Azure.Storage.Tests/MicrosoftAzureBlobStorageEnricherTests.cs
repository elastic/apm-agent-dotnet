// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities.Azure;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Storage.Tests
{
	[Collection("AzureStorage")]
	public class MicrosoftAzureBlobStorageEnricherTests : BlobStorageTestsBase
	{
		private readonly CloudStorageAccount _account;

		public MicrosoftAzureBlobStorageEnricherTests(AzureStorageTestEnvironment environment, ITestOutputHelper output)
			:base(environment, output) =>
			_account = CloudStorageAccount.Parse(Environment.StorageAccountConnectionString);

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Container()
		{
			var containerName = Guid.NewGuid().ToString();
			var client = _account.CreateCloudBlobClient();

			await Agent.Tracer.CaptureTransaction("Create Azure Container", AzureBlobStorage.Type, async () =>
			{
				await client.GetContainerReference(containerName).CreateAsync();
			});

			AssertSpan("Create", containerName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Container()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);
			var client = _account.CreateCloudBlobClient();

			await Agent.Tracer.CaptureTransaction("Delete Azure Container", AzureBlobStorage.Type, async () =>
			{
				await client.GetContainerReference(scope.ContainerName).DeleteAsync();
			});

			AssertSpan("Delete", scope.ContainerName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upload_Block_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);
			var client = _account.CreateCloudBlobClient();
			var containerReference = client.GetContainerReference(scope.ContainerName);
			var blobName = Guid.NewGuid().ToString();
			var blobReference = containerReference.GetBlockBlobReference(blobName);

			await Agent.Tracer.CaptureTransaction("Upload Azure Block Blob", AzureBlobStorage.Type, async () =>
			{
				var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
				await blobReference.UploadFromStreamAsync(stream);
			});

			AssertSpan("Upload", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Download_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);
			var client = _account.CreateCloudBlobClient();
			var containerReference = client.GetContainerReference(scope.ContainerName);
			var blobName = Guid.NewGuid().ToString();

			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await scope.ContainerClient.UploadBlobAsync(blobName, stream);

			await Agent.Tracer.CaptureTransaction("Download Azure Block Blob", AzureBlobStorage.Type, async () =>
			{
				var blobReference = containerReference.GetBlockBlobReference(blobName);
				var downloadResponse = await blobReference.DownloadTextAsync();
			});

			AssertSpan("Download", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await scope.ContainerClient.UploadBlobAsync(blobName, stream);

			var client = _account.CreateCloudBlobClient();
			var containerReference = client.GetContainerReference(scope.ContainerName);

			await Agent.Tracer.CaptureTransaction("Delete Azure Blob", AzureBlobStorage.Type, async () =>
			{
				var blobReference = containerReference.GetBlockBlobReference(blobName);
				await blobReference.DeleteAsync();
			});

			AssertSpan("Delete", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Get_Blobs()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			for (var i = 0; i < 2; i++)
			{
				var blobName = Guid.NewGuid().ToString();
				await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
				var blobUploadResponse = await scope.ContainerClient.UploadBlobAsync(blobName, stream);
			}

			var client = _account.CreateCloudBlobClient();
			var containerReference = client.GetContainerReference(scope.ContainerName);

			await Agent.Tracer.CaptureTransaction("Get Blobs", AzureBlobStorage.Type, async () =>
			{
				var segment = await containerReference.ListBlobsSegmentedAsync(null);
			});

			AssertSpan("GetBlobs", scope.ContainerName);
		}
	}
}
