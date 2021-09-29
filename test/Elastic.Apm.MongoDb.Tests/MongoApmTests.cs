// Based on the elastic-apm-mongo project by Vadim Hatsura (@vhatsura)
// https://github.com/vhatsura/elastic-apm-mongo
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.MongoDb.Tests.Fixture;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace Elastic.Apm.MongoDb.Tests
{
	public class MongoApmTests : IClassFixture<MongoFixture<MongoApmTests.MongoConfiguration, BsonDocument>>,
		IDisposable
	{
		public MongoApmTests(MongoFixture<MongoConfiguration, BsonDocument> fixture)
		{
			_documents = fixture.Collection ?? throw new ArgumentNullException(nameof(fixture.Collection));
			_payloadSender = new MockPayloadSender();

			var config = new TestAgentComponents(configuration:
				new MockConfiguration(transactionSampleRate: "1.0", transactionMaxSpans: "50"),
				payloadSender: _payloadSender);

			_agent = new ApmAgent(config);
			_agent.Subscribe(new MongoDbDiagnosticsSubscriber());
		}

		public void Dispose() => (_agent as IDisposable)?.Dispose();

		private readonly IApmAgent _agent;

		private const string DatabaseName = "elastic-apm-mongo";

		public class MongoConfiguration : IMongoConfiguration<BsonDocument>
		{
			public MongoClient GetMongoClient(string connectionString)
			{
				var mongoClientSettings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
				mongoClientSettings.ClusterConfigurator = builder => builder.Subscribe(new MongoDbEventSubscriber());

				return new MongoClient(mongoClientSettings);
			}

			string IMongoConfiguration<BsonDocument>.DatabaseName => DatabaseName;
			public string CollectionName => "documents";

			public Task InitializeAsync(IMongoCollection<BsonDocument> collection) =>
				collection.InsertManyAsync(Enumerable.Range(0, 10_000).Select(x => new BsonDocument("_id", x)));

			public Task DisposeAsync(IMongoCollection<BsonDocument> collection) =>
				collection.Database.DropCollectionAsync(CollectionName);
		}

		private readonly IMongoCollection<BsonDocument> _documents;

		private readonly MockPayloadSender _payloadSender;

		[Fact]
		public async Task ApmAgent_ShouldCorrectlyCaptureSpan()
		{
			// Arrange
			var transaction = _agent.Tracer.StartTransaction("elastic-apm-mongo", ApiConstants.TypeDb);

			// Act
			var docs = await _documents
				.Find(Builders<BsonDocument>.Filter.Empty)
				.Project(Builders<BsonDocument>.Projection.ElemMatch("filter", FilterDefinition<BsonDocument>.Empty))
				.ToListAsync();

			transaction.End();

			// Assert
			_payloadSender.Transactions.Should().NotBeNullOrEmpty();
			_payloadSender.FirstTransaction.Should().NotBeNull();

			var (address, port) = GetDestination(_documents.Database.Client);

			_payloadSender.Spans.Should().NotBeEmpty();
			_payloadSender.Spans.ForEach(span =>
			{
				span.TransactionId.Should().Be(_payloadSender.FirstTransaction.Id);
				span.Context.Db.Instance.Should().Be(DatabaseName);
				span.Type.Should().Be(ApiConstants.TypeDb);

				span.Context.Destination.Should().NotBeNull();
				span.Context.Destination.Address.Should().Be(address);
				span.Context.Destination.Port.Should().Be(port);

				span.ParentId.Should().Be(_payloadSender.FirstTransaction.Id);
			});
		}

		[Fact]
		public async Task ApmAgent_ShouldCorrectlyCaptureSpanAndError_WhenMongoCommandFailed()
		{
			// Arrange
			var transaction = _agent.Tracer.StartTransaction("elastic-apm-mongo", ApiConstants.TypeDb);

			// Act
			try
			{
				// run failPoint command on non-admin database which is forbidden
				await _documents.Database.RunCommandAsync(new JsonCommand<BsonDocument>(
					"{configureFailPoint: \"failCommand\", mode: \"alwaysOn\",data: {errorCode: 2, failCommands: [\"find\"]}}"));
				//await _documents.Database.RunCommandAsync(new JsonCommand<BsonDocument>("{}"));
			}
			catch
			{
				// ignore
			}

			transaction.End();

			// Assert
			_payloadSender.Transactions.Should().NotBeNullOrEmpty();
			_payloadSender.FirstTransaction.Should().NotBeNull();

			_payloadSender.Spans.Should().NotBeNullOrEmpty();
			_payloadSender.FirstSpan.Should().NotBeNull();

			_payloadSender.FirstSpan.TransactionId.Should().Be(_payloadSender.FirstTransaction.Id);
			_payloadSender.FirstSpan.Context.Db.Instance.Should().Be(DatabaseName);
			_payloadSender.FirstSpan.Type.Should().Be(ApiConstants.TypeDb);

			var (address, port) = GetDestination(_documents.Database.Client);

			_payloadSender.FirstSpan.Context.Destination.Should().NotBeNull();
			_payloadSender.FirstSpan.Context.Destination.Address.Should().Be(address);
			_payloadSender.FirstSpan.Context.Destination.Port.Should().Be(port);


			_payloadSender.Errors.Should().NotBeNullOrEmpty();
			_payloadSender.FirstError.Should().NotBeNull();

			_payloadSender.FirstError.TransactionId.Should().Be(_payloadSender.FirstTransaction.Id);
			_payloadSender.FirstError.ParentId.Should().Be(_payloadSender.FirstSpan.Id);
		}

		private static (string Address, int Port) GetDestination(IMongoClient mongoClient)
		{
			// in case of connection to replica set with multiple nodes `Servers` property should be checked
			var serverAddress = mongoClient.Settings.Server;

			return (serverAddress.Host, serverAddress.Port);
		}
	}
}
