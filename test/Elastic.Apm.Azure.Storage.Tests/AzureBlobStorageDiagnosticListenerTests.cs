using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities.Azure;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Storage.Tests
{
	[Collection("AzureStorage")]
	public class AzureBlobStorageDiagnosticListenerTests : BlobStorageTestsBase
	{
		public AzureBlobStorageDiagnosticListenerTests(AzureStorageTestEnvironment environment, ITestOutputHelper output)
			:base (environment, output)
		{
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Container()
		{
			var containerName = Guid.NewGuid().ToString();
			var client = new BlobContainerClient(Environment.StorageAccountConnectionString, containerName);

			await Agent.Tracer.CaptureTransaction("Create Azure Container", ApiConstants.TypeStorage, async () =>
			{
				var containerCreateResponse = await client.CreateAsync();
			});

			AssertSpan("Create", containerName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Container()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			await Agent.Tracer.CaptureTransaction("Delete Azure Container", ApiConstants.TypeStorage, async () =>
			{
				var containerDeleteResponse = await scope.ContainerClient.DeleteAsync();
			});

			AssertSpan("Delete", scope.ContainerName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Page_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = new PageBlobClient(Environment.StorageAccountConnectionString, scope.ContainerName, blobName);

			await Agent.Tracer.CaptureTransaction("Create Azure Page Blob", ApiConstants.TypeStorage, async () =>
			{
				var blobCreateResponse = await client.CreateAsync(1024);
			});

			AssertSpan("Create", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upload_Page_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = new PageBlobClient(Environment.StorageAccountConnectionString, scope.ContainerName, blobName);
			var blobCreateResponse = await client.CreateAsync(1024);

			await Agent.Tracer.CaptureTransaction("Upload Azure Page Blob", ApiConstants.TypeStorage, async () =>
			{
				var random = new Random();
				var bytes = new byte[512];
				random.NextBytes(bytes);

				var stream = new MemoryStream(bytes);
				var uploadPagesResponse = await client.UploadPagesAsync(stream, 0);
			});

			AssertSpan("Upload", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upload_Block_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = new BlockBlobClient(Environment.StorageAccountConnectionString, scope.ContainerName, blobName);

			await Agent.Tracer.CaptureTransaction("Upload Azure Block Blob", ApiConstants.TypeStorage, async () =>
			{
				var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
				var blobUploadResponse = await client.UploadAsync(stream);
			});

			AssertSpan("Upload", $"{scope.ContainerName}/{blobName}");
		}

		private class UnSeekableStream : MemoryStream
		{
			public UnSeekableStream()
			{
			}
			public UnSeekableStream(byte[] buffer) : base(buffer)
			{
			}

			public override bool CanSeek => false;
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upload_Block_Blob_With_Unseekable_Stream()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = new BlockBlobClient(Environment.StorageAccountConnectionString, scope.ContainerName, blobName);

			await Agent.Tracer.CaptureTransaction("Upload Azure Block Blob", ApiConstants.TypeStorage, async () =>
			{
				var stream = new UnSeekableStream(Encoding.UTF8.GetBytes("block blob"));
				var blobUploadResponse = await client.UploadAsync(stream);
			});

			AssertSpan("Upload", $"{scope.ContainerName}/{blobName}", count: 3);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Download_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = scope.ContainerClient.GetBlobClient(blobName);

			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await client.UploadAsync(stream);

			await Agent.Tracer.CaptureTransaction("Download Azure Block Blob", ApiConstants.TypeStorage, async () =>
			{
				var downloadResponse = await client.DownloadAsync();
			});

			AssertSpan("Download", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Download_Streaming_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = scope.ContainerClient.GetBlobClient(blobName);

			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await client.UploadAsync(stream);

			await Agent.Tracer.CaptureTransaction("Download Azure Block Blob", ApiConstants.TypeStorage, async () =>
			{
				stream.Position = 0;
				var downloadResponse = await client.DownloadToAsync(stream);
			});

			AssertSpan("Download", $"{scope.ContainerName}/{blobName}", count: 2);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await scope.ContainerClient.UploadBlobAsync(blobName, stream);

			await Agent.Tracer.CaptureTransaction("Delete Azure Blob", ApiConstants.TypeStorage, async () =>
			{
				var containerDeleteResponse = await scope.ContainerClient.DeleteBlobAsync(blobName);
			});

			AssertSpan("Delete", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Copy_From_Uri()
		{
			await using var scope = await BlobContainerScope.CreateContainer(Environment.StorageAccountConnectionString);

			var sourceBlobName = Guid.NewGuid().ToString();
			var client = scope.ContainerClient.GetBlobClient(sourceBlobName);

			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await client.UploadAsync(stream);

			var destinationBlobName = Guid.NewGuid().ToString();
			await Agent.Tracer.CaptureTransaction("Copy Azure Blob", ApiConstants.TypeStorage, async () =>
			{
				var otherClient = scope.ContainerClient.GetBlobClient(destinationBlobName);
				var operation = await otherClient.StartCopyFromUriAsync(client.Uri);
				await operation.WaitForCompletionAsync();
			});

			AssertSpan("Copy", $"{scope.ContainerName}/{destinationBlobName}", count: 2);
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

			await Agent.Tracer.CaptureTransaction("Get Blobs", ApiConstants.TypeStorage, async () =>
			{
				var asyncPageable = scope.ContainerClient.GetBlobsAsync();
				await foreach (var blob in asyncPageable)
				{
					// ReSharper disable once Xunit.XunitTestWithConsoleOutput
					Console.WriteLine(blob.Name);
				}
			});

			AssertSpan("ListBlobs", scope.ContainerName);
		}
	}
}
