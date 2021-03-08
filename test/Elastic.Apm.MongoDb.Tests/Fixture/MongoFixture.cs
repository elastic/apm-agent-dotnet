using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Elastic.Apm.Mongo.IntegrationTests.Fixture.TestEnvironment.Docker;
using MongoDB.Driver;
using TestEnvironment.Docker;
using Xunit;

namespace Elastic.Apm.Mongo.IntegrationTests.Fixture
{
	public class MongoFixture<TConfiguration, TDocument> : IAsyncLifetime, IDisposable
		where TConfiguration : IMongoConfiguration<TDocument>, new()
	{
		private readonly TConfiguration _configuration;
		private readonly DockerEnvironment _environment;

		public MongoFixture()
		{
			_configuration = new TConfiguration();

			_environment = new DockerEnvironmentBuilder()
				//.DockerInDocker(Environment.GetEnvironmentVariable("TF_BUILD") != null)
				.AddMongoContainer("mongo")
				.Build();
		}

		public IMongoCollection<TDocument>? Collection { get; private set; }

		public async Task InitializeAsync()
		{
			await _environment.Up();

			var mongoContainer = _environment.GetContainer<MongoContainer>("mongo");

			var mongoClient = _configuration.GetMongoClient(mongoContainer.GetConnectionString());
			Collection = mongoClient.GetDatabase(_configuration.DatabaseName)
				.GetCollection<TDocument>(_configuration.CollectionName);

			await _configuration.InitializeAsync(Collection);
		}

		public async Task DisposeAsync()
		{
			if (Collection != null)
			{
				await _configuration.DisposeAsync(Collection);
			}

			await _environment.Down();
			await _environment.DisposeAsync();
		}

		public void Dispose()
		{
		}
	}
}
