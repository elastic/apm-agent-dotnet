using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DiagnosticSource;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Azure;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Microsoft.Azure.Cosmos;
using Xunit;
using Xunit.Abstractions;
using Container = Microsoft.Azure.Cosmos.Container;
using Database = Microsoft.Azure.Cosmos.Database;

namespace Elastic.Apm.Azure.CosmosDb.Tests
{
	[Collection("AzureCosmosDb")]
	public class MicrosoftAzureCosmosTests
	{
		private readonly ITestOutputHelper _output;
		private readonly MockPayloadSender _sender;
		private readonly ApmAgent _agent;
		private readonly CosmosClient _client;

		public MicrosoftAzureCosmosTests(AzureCosmosDbTestEnvironment environment, ITestOutputHelper output)
		{
			_output = output;

			var logger = new XUnitLogger(LogLevel.Trace, output);
			_sender = new MockPayloadSender(logger);
			_agent = new ApmAgent(new TestAgentComponents(logger: logger, payloadSender: _sender));
			_agent.Subscribe(new AzureCosmosDbDiagnosticsSubscriber());
			_client = new CosmosClient(environment.Endpoint, environment.PrimaryMasterKey, new CosmosClientOptions
			{
				ConnectionMode = ConnectionMode.Gateway
			});
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
				await db.DeleteAsync();
			});

			AssertSpan($"Delete database {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Get_Database()
		{
			var db = await CreateDatabaseAsync();
			await _agent.Tracer.CaptureTransaction("Get CosmosDb Database", ApiConstants.TypeDb, async () =>
			{
				await db.ReadAsync();
			});

			AssertSpan($"Get database {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_List_Databases()
		{
			await _agent.Tracer.CaptureTransaction("List CosmosDb Databases", ApiConstants.TypeDb, async () =>
			{
				var iterator = _client.GetDatabaseQueryIterator<DatabaseProperties>();
				while(iterator.HasMoreResults)
				{
					foreach (var db in await iterator.ReadNextAsync()) { }
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
				await CreateContainerAsync(db);
			});

			AssertSpan($"Create collection {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Collection()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateContainerAsync(db);

			await _agent.Tracer.CaptureTransaction("Delete CosmosDb Collection", ApiConstants.TypeDb, async () =>
			{
				await container.DeleteContainerAsync();
			});

			AssertSpan($"Delete collection {db.Id} {container.Id}", db.Id);
		}

		private string RandomName() => Guid.NewGuid().ToString("N");

		[AzureCredentialsFact]
		public async Task Capture_Span_When_List_Collections()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateContainerAsync(db);

			await _agent.Tracer.CaptureTransaction("List CosmosDb Collections", ApiConstants.TypeDb, async () =>
			{
				var iterator = db.GetContainerQueryIterator<ContainerProperties>();
				while(iterator.HasMoreResults)
				{
					foreach (var container in await iterator.ReadNextAsync())
					{
					}
				}
			});

			AssertSpan($"List collections {db.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Create_Document()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateContainerAsync(db);

			await _agent.Tracer.CaptureTransaction("Create CosmosDb Item", ApiConstants.TypeDb, async () =>
			{
				var item = new DocumentItem { Id = "1", FirstName = "Russ", LastName = "Cam" };
				var itemResponse = await container.CreateItemAsync(item);
				((int)itemResponse.StatusCode).Should().BeInRange(200, 299);
			});

			AssertSpan($"Create/query document {db.Id} {container.Id}", db.Id, 2);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Upsert_Document()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateContainerAsync(db);

			await _agent.Tracer.CaptureTransaction("Create CosmosDb Item", ApiConstants.TypeDb, async () =>
			{
				var item = new DocumentItem { Id = "1", FirstName = "Russ", LastName = "Cam" };
				var itemResponse = await container.UpsertItemAsync(item);
				((int)itemResponse.StatusCode).Should().BeInRange(200, 299);
			});

			AssertSpan($"Create/query document {db.Id} {container.Id}", db.Id, 2);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Delete_Document()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateContainerAsync(db);
			var item = new DocumentItem { Id = "2", FirstName = "Greg", LastName = "Kalapos" };
			var itemResponse = await container.CreateItemAsync(item, new PartitionKey(item.PartitionKey));

			((int)itemResponse.StatusCode).Should().BeInRange(200, 299);

			await _agent.Tracer.CaptureTransaction("Delete CosmosDb Item", ApiConstants.TypeDb, async () =>
			{
				await container.DeleteItemAsync<object>(item.Id, new PartitionKey(item.PartitionKey));
			});

			AssertSpan($"Delete document {db.Id} {container.Id}", db.Id);
		}

		[AzureCredentialsFact]
		public async Task Capture_Span_When_Replace_Document()
		{
			var db = await CreateDatabaseAsync();
			var container = await CreateContainerAsync(db);
			var item = new DocumentItem { Id = "2", FirstName = "Greg", LastName = "Kalapos" };
			var itemResponse = await container.CreateItemAsync(item);
			((int)itemResponse.StatusCode).Should().BeInRange(200, 299);

			item.FirstName = "Gerg";
			await _agent.Tracer.CaptureTransaction("Upsert CosmosDb Item", ApiConstants.TypeDb, async () =>
			{
				var response = await container.ReplaceItemAsync(item, item.Id);
				((int)response.StatusCode).Should().BeInRange(200, 299);
			});

			AssertSpan($"Replace document {db.Id} {container.Id}", db.Id);
		}

		private async Task<Container> CreateContainerAsync(Database db)
		{
			var containerId = RandomName();
			var containerResponse = await db.CreateContainerAsync(containerId, "/PartitionKey");
			return containerResponse.Container;
		}

		private async Task<Database> CreateDatabaseAsync()
		{
			var databaseName = RandomName();
			var response = await _client.CreateDatabaseAsync(databaseName);
			return response.Database;
		}

		private void AssertSpan(string action, string db = null, int count = 1)
		{
			if (!_sender.WaitForSpans())
				throw new Exception("No span received in timeout");

			_sender.Spans.Should().HaveCount(count);
			var span = _sender.Spans.Last();

			span.Name.Should().Be(string.IsNullOrEmpty(action) ? $"Cosmos DB" : $"Cosmos DB {action}");
			span.Type.Should().Be(ApiConstants.TypeDb);
			span.Subtype.Should().Be(ApiConstants.SubTypeCosmosDb);

			if (db != null)
			{
				span.Context.Db.Instance.Should().Be(db);
				span.Context.Destination.Service.Resource.Should().Be($"{ApiConstants.SubTypeCosmosDb}:{db}");
			}
			else
				span.Context.Destination.Service.Resource.Should().Be($"{ApiConstants.SubTypeCosmosDb}");
		}
	}
}
