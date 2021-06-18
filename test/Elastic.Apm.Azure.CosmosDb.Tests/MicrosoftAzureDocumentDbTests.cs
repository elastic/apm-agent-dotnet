using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Azure.CosmosDb.Tests
{
	[Collection("AzureCosmosDb")]
	public class MicrosoftAzureDocumentDbTests
	{
		private readonly MockPayloadSender _sender;
		private readonly ApmAgent _agent;
		private readonly DocumentClient _client;

		public MicrosoftAzureDocumentDbTests(AzureCosmosDbTestEnvironment environment, ITestOutputHelper output)
		{
			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new AzureCosmosDbDiagnosticsSubscriber());
			_client = new DocumentClient(new Uri(environment.Endpoint), environment.PrimaryMasterKey);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Database()
		{
			await _agent.Tracer.CaptureTransaction("Create CosmosDb Database", ApiConstants.TypeDb, async () =>
			{
				await CreateDatabaseAsync();
			});

			AssertSpan("Create database");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Database()
		{
			var db = await CreateDatabaseAsync();
			await _agent.Tracer.CaptureTransaction("Delete CosmosDb Database", ApiConstants.TypeDb, async () =>
			{
				await _client.DeleteDatabaseAsync(db.AltLink);
			});

			AssertSpan($"Delete database {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Get_Database()
		{
			var db = await CreateDatabaseAsync();
			await _agent.Tracer.CaptureTransaction("Get CosmosDb Database", ApiConstants.TypeDb, async () =>
			{
				await _client.ReadDatabaseAsync(db.AltLink);
			});

			AssertSpan($"Get database {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_List_Databases()
		{
			await _agent.Tracer.CaptureTransaction("List CosmosDb Databases", ApiConstants.TypeDb, async () =>
			{
				var response = await _client.ReadDatabaseFeedAsync();
				foreach(var db in response)
				{
				}
			});

			AssertSpan("List databases");
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Collection()
		{
			var db = await CreateDatabaseAsync();
			await _agent.Tracer.CaptureTransaction("Create CosmosDb Collection", ApiConstants.TypeDb, async () =>
			{
				await CreateCollectionAsync(db);
			});

			AssertSpan($"Create collection {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Collection()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateCollectionAsync(db);

			await _agent.Tracer.CaptureTransaction("Delete CosmosDb Collection", ApiConstants.TypeDb, async () =>
			{
				await _client.DeleteDocumentCollectionAsync(container.AltLink);
			});

			AssertSpan($"Delete collection {db.Id} {container.Id}", db.Id);
		}

		private string RandomName() => Guid.NewGuid().ToString("N");

		[AzureCredentialsFact]
		public async Task Capture_Span_When_List_Collections()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateCollectionAsync(db);

			await _agent.Tracer.CaptureTransaction("List CosmosDb Collections", ApiConstants.TypeDb, async () =>
			{
				var response = await _client.ReadDocumentCollectionFeedAsync(db.AltLink);
				foreach (var collection in response) { }
			});

			AssertSpan($"List collections {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Document()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateCollectionAsync(db);
			var link = UriFactory.CreateDocumentCollectionUri(db.Id, container.Id);

			await _agent.Tracer.CaptureTransaction("Create CosmosDb Item", ApiConstants.TypeDb, async () =>
			{
				var item = new DocumentItem { Id = "1", FirstName = "Russ", LastName = "Cam" };
				await _client.CreateDocumentAsync(link, item);
			});

			AssertSpan($"Create/query document {db.Id} {container.Id}", db.Id, 2);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Document()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateCollectionAsync(db);
			var item = new DocumentItem { Id = "2", FirstName = "Greg", LastName = "Kalapos" };
			var response = await _client.CreateDocumentAsync(container.DocumentsLink, item);

			await _agent.Tracer.CaptureTransaction("Delete CosmosDb Item", ApiConstants.TypeDb, async () =>
			{
				await _client.DeleteDocumentAsync(
					response.Resource.AltLink,
					new RequestOptions { PartitionKey = new PartitionKey(item.PartitionKey) });
			});

			AssertSpan($"Delete document {db.Id} {container.Id}", db.Id, 2);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upsert_Document()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateCollectionAsync(db);
			var item = new DocumentItem { Id = "2", FirstName = "Greg", LastName = "Kalapos" };
			var response = await _client.CreateDocumentAsync(container.DocumentsLink, item);

			await _agent.Tracer.CaptureTransaction("Upsert CosmosDb Item", ApiConstants.TypeDb, async () =>
			{
				var newItem = new DocumentItem { Id = item.Id, FirstName = "Gerg", LastName = item.LastName };
				await _client.ReplaceDocumentAsync(response.Resource.AltLink, newItem);
			});

			AssertSpan($"Replace document {db.Id} {container.Id}", db.Id, 2);
		}

		private async Task<DocumentCollection> CreateCollectionAsync(Microsoft.Azure.Documents.Database db)
		{
			var containerId = RandomName();
			var containerResponse = await _client.CreateDocumentCollectionAsync(
				db.AltLink,
				new DocumentCollection {
					Id = containerId,
					PartitionKey = new PartitionKeyDefinition{ Paths = new Collection<string>{ "/PartitionKey" } } });
			return containerResponse.Resource;
		}

		private async Task<Microsoft.Azure.Documents.Database> CreateDatabaseAsync()
		{
			var db = new Microsoft.Azure.Documents.Database { Id = RandomName() };
			var response = await _client.CreateDatabaseAsync(db);
			return response.Resource;
		}

		private void AssertSpan(string action, string db = null, int count = 1)
		{
			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(count);
			var span = _sender.Spans.Last();

			span.Name.Should().Be($"Cosmos DB {action}");
			span.Type.Should().Be(ApiConstants.TypeDb);
			span.Subtype.Should().Be(ApiConstants.SubTypeCosmosDb);

			if (db != null)
				span.Context.Db.Instance.Should().Be(db);
		}
	}
}
