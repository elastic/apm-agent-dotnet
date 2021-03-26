using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Storage.Tests
{
	[Collection("AzureStorage")]
	public class AzureBlobStorageDiagnosticListenerTests
	{
		private readonly AzureStorageTestEnvironment _environment;
		private readonly ITestOutputHelper _testOutputHelper;
		private readonly MockPayloadSender _sender;
		private readonly ApmAgent _agent;

		public AzureBlobStorageDiagnosticListenerTests(AzureStorageTestEnvironment environment, ITestOutputHelper output, ITestOutputHelper testOutputHelper)
		{
			_environment = environment;
			_testOutputHelper = testOutputHelper;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new AzureBlobStorageDiagnosticsSubscriber());
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Container()
		{
			var containerName = Guid.NewGuid().ToString();
			var client = new BlobContainerClient(_environment.StorageAccountConnectionString, containerName);

			await _agent.Tracer.CaptureTransaction("Create Azure Container", AzureBlobStorage.Type, async () =>
			{
				var containerCreateResponse = await client.CreateAsync();
			});

			AssertSpan("Create", containerName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Container()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			await _agent.Tracer.CaptureTransaction("Delete Azure Container", AzureBlobStorage.Type, async () =>
			{
				var containerDeleteResponse = await scope.ContainerClient.DeleteAsync();
			});

			AssertSpan("Delete", scope.ContainerName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Page_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = new PageBlobClient(_environment.StorageAccountConnectionString, scope.ContainerName, blobName);

			await _agent.Tracer.CaptureTransaction("Create Azure Page Blob", AzureBlobStorage.Type, async () =>
			{
				var blobCreateResponse = await client.CreateAsync(1024);
			});

			AssertSpan("Create", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upload_Page_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = new PageBlobClient(_environment.StorageAccountConnectionString, scope.ContainerName, blobName);
			var blobCreateResponse = await client.CreateAsync(1024);

			await _agent.Tracer.CaptureTransaction("Upload Azure Page Blob", AzureBlobStorage.Type, async () =>
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
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = new BlockBlobClient(_environment.StorageAccountConnectionString, scope.ContainerName, blobName);

			await _agent.Tracer.CaptureTransaction("Upload Azure Block Blob", AzureBlobStorage.Type, async () =>
			{
				var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
				var blobUploadResponse = await client.UploadAsync(stream);
			});

			AssertSpan("Upload", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Download_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = scope.ContainerClient.GetBlobClient(blobName);

			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await client.UploadAsync(stream);

			await _agent.Tracer.CaptureTransaction("Download Azure Block Blob", AzureBlobStorage.Type, async () =>
			{
				var downloadResponse = await client.DownloadAsync();
			});

			AssertSpan("Download", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Download_Streaming_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			var client = scope.ContainerClient.GetBlobClient(blobName);

			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await client.UploadAsync(stream);

			await _agent.Tracer.CaptureTransaction("Download Azure Block Blob", AzureBlobStorage.Type, async () =>
			{
				stream.Position = 0;
				var downloadResponse = await client.DownloadToAsync(stream);
			});

			AssertSpan("Download", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Blob()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var blobName = Guid.NewGuid().ToString();
			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await scope.ContainerClient.UploadBlobAsync(blobName, stream);

			await _agent.Tracer.CaptureTransaction("Delete Azure Blob", AzureBlobStorage.Type, async () =>
			{
				var containerDeleteResponse = await scope.ContainerClient.DeleteBlobAsync(blobName);
			});

			AssertSpan("Delete", $"{scope.ContainerName}/{blobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Copy_From_Uri()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var sourceBlobName = Guid.NewGuid().ToString();
			var client = scope.ContainerClient.GetBlobClient(sourceBlobName);

			await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
			var blobUploadResponse = await client.UploadAsync(stream);

			var destinationBlobName = Guid.NewGuid().ToString();
			await _agent.Tracer.CaptureTransaction("Copy Azure Blob", AzureBlobStorage.Type, async () =>
			{
				var otherClient = scope.ContainerClient.GetBlobClient(destinationBlobName);
				var operation = await otherClient.StartCopyFromUriAsync(client.Uri);
				await operation.WaitForCompletionAsync();
			});

			AssertSpan("CopyFromUri", $"{scope.ContainerName}/{destinationBlobName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Get_Blobs()
		{
			await using var scope = await BlobContainerScope.CreateContainer(_environment.StorageAccountConnectionString);

			var c = scope.ContainerClient.GetBlobClient("fo");

			for (var i = 0; i < 2; i++)
			{
				var blobName = Guid.NewGuid().ToString();
				await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("block blob"));
				var blobUploadResponse = await scope.ContainerClient.UploadBlobAsync(blobName, stream);
			}

			await _agent.Tracer.CaptureTransaction("Get Blobs", AzureBlobStorage.Type, async () =>
			{
				var asyncPageable = scope.ContainerClient.GetBlobsAsync();
				await foreach (var blob in asyncPageable)
				{
					// ReSharper disable once Xunit.XunitTestWithConsoleOutput
					Console.WriteLine(blob.Name);
				}
			});

			AssertSpan("GetBlobs", scope.ContainerName);
		}

		private void AssertSpan(string action, string resource)
		{
			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{AzureBlobStorage.SpanName} {action} {resource}");
			span.Type.Should().Be(AzureBlobStorage.Type);
			span.Subtype.Should().Be(AzureBlobStorage.SubType);
			span.Action.Should().Be(action);
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.StorageAccountConnectionStringProperties.BlobUrl);
			destination.Service.Name.Should().Be(AzureBlobStorage.SubType);
			destination.Service.Resource.Should().Be($"{AzureBlobStorage.SubType}/{resource}");
			destination.Service.Type.Should().Be(AzureBlobStorage.Type);
		}
	}
}
