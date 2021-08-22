using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.Storage.Tests
{
	[Collection("AzureStorage")]
	public class AzureFileShareStorageDiagnosticListenerTests
	{
		private readonly AzureStorageTestEnvironment _environment;
		private readonly MockPayloadSender _sender;
		private readonly ApmAgent _agent;

		public AzureFileShareStorageDiagnosticListenerTests(AzureStorageTestEnvironment environment, ITestOutputHelper output)
		{
			_environment = environment;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new AzureFileShareStorageDiagnosticsSubscriber());
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_File_Share()
		{
			var shareName = Guid.NewGuid().ToString();
			var client = new ShareClient(_environment.StorageAccountConnectionString, shareName);

			await _agent.Tracer.CaptureTransaction("Create Azure File Share", ApiConstants.TypeStorage, async () =>
			{
				var response = await client.CreateAsync();
			});


			AssertSpan("Create", shareName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_File_Share()
		{
			await using var scope = await FileShareScope.CreateShare(_environment.StorageAccountConnectionString);

			await _agent.Tracer.CaptureTransaction("Delete Azure File Share", ApiConstants.TypeStorage, async () =>
			{
				var deleteResponse = await scope.ShareClient.DeleteAsync();
			});

			AssertSpan("Delete", scope.ShareName);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_File_Share_Directory()
		{
			await using var scope = await FileShareScope.CreateShare(_environment.StorageAccountConnectionString);
			var directoryName = Guid.NewGuid().ToString();
			var client = scope.ShareClient.GetDirectoryClient(directoryName);

			await _agent.Tracer.CaptureTransaction("Create Azure File Share Directory", ApiConstants.TypeStorage, async () =>
			{
				var response = await client.CreateAsync();
			});

			AssertSpan("Create", $"{scope.ShareName}/{directoryName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_File_Share_Directory()
		{
			await using var scope = await FileShareScope.CreateShare(_environment.StorageAccountConnectionString);
			var directoryName = Guid.NewGuid().ToString();
			var client = scope.ShareClient.GetDirectoryClient(directoryName);
			var createResponse = await client.CreateAsync();

			await _agent.Tracer.CaptureTransaction("Delete Azure File Share Directory", ApiConstants.TypeStorage, async () =>
			{
				var deleteResponse = await client.DeleteAsync();
			});

			AssertSpan("Delete", $"{scope.ShareName}/{directoryName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_File_Share_File()
		{
			await using var scope = await FileShareScope.CreateDirectory(_environment.StorageAccountConnectionString);
			var fileName = Guid.NewGuid().ToString();
			var client = scope.DirectoryClient.GetFileClient(fileName);

			await _agent.Tracer.CaptureTransaction("Create Azure File Share File", ApiConstants.TypeStorage, async () =>
			{
				await client.CreateAsync(1024);
			});

			AssertSpan("Create", $"{scope.ShareName}/{scope.DirectoryName}/{fileName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_File_Share_File()
		{
			await using var scope = await FileShareScope.CreateDirectory(_environment.StorageAccountConnectionString);
			var fileName = Guid.NewGuid().ToString();
			var client = scope.DirectoryClient.GetFileClient(fileName);
			var createResponse = await client.CreateAsync(1024);

			await _agent.Tracer.CaptureTransaction("Delete Azure File Share File", ApiConstants.TypeStorage, async () =>
			{
				var response = await client.DeleteAsync();
			});

			AssertSpan("Delete", $"{scope.ShareName}/{scope.DirectoryName}/{fileName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_UploadRange_File_Share_File()
		{
			await using var scope = await FileShareScope.CreateDirectory(_environment.StorageAccountConnectionString);
			var fileName = Guid.NewGuid().ToString();
			var client = scope.DirectoryClient.GetFileClient(fileName);

			var bytes = Encoding.UTF8.GetBytes("temp file");
			var createResponse = await client.CreateAsync(bytes.Length);

			await _agent.Tracer.CaptureTransaction("Delete Azure File Share File", ApiConstants.TypeStorage, async () =>
			{
				await using var stream = new MemoryStream(bytes);
				var response = await client.UploadRangeAsync(new HttpRange(0, bytes.Length), stream);
			});

			AssertSpan("Upload", $"{scope.ShareName}/{scope.DirectoryName}/{fileName}");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upload_File_Share_File()
		{
			await using var scope = await FileShareScope.CreateDirectory(_environment.StorageAccountConnectionString);
			var fileName = Guid.NewGuid().ToString();
			var client = scope.DirectoryClient.GetFileClient(fileName);

			var bytes = Encoding.UTF8.GetBytes("temp file");
			var createResponse = await client.CreateAsync(bytes.Length);

			await _agent.Tracer.CaptureTransaction("Delete Azure File Share File", ApiConstants.TypeStorage, async () =>
			{
				await using var stream = new MemoryStream(bytes);
				var response = await client.UploadAsync(stream);
			});

			AssertSpan("Upload", $"{scope.ShareName}/{scope.DirectoryName}/{fileName}");
		}

		private void AssertSpan(string action, string resource)
		{
			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(1);
			var span = _sender.FirstSpan;

			span.Name.Should().Be($"{AzureFileStorage.SpanName} {action} {resource}");
			span.Type.Should().Be(ApiConstants.TypeStorage);
			span.Subtype.Should().Be(AzureFileStorage.SubType);
			span.Action.Should().Be(action);
			span.Context.Destination.Should().NotBeNull();
			var destination = span.Context.Destination;

			destination.Address.Should().Be(_environment.StorageAccountConnectionStringProperties.FileFullyQualifiedNamespace);
			destination.Service.Name.Should().Be(AzureFileStorage.SubType);
			destination.Service.Resource.Should().Be($"{AzureFileStorage.SubType}/{_environment.StorageAccountConnectionStringProperties.AccountName}");
			destination.Service.Type.Should().Be(ApiConstants.TypeStorage);
		}
	}
}
