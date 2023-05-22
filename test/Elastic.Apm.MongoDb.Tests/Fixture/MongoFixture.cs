// Based on the elastic-apm-mongo project by Vadim Hatsura (@vhatsura)
// https://github.com/vhatsura/elastic-apm-mongo
// Licensed to Elasticsearch B.V under the Apache 2.0 License.
// Elasticsearch B.V licenses this file, including any modifications, to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Xunit;

namespace Elastic.Apm.MongoDb.Tests.Fixture
{
	public class MongoFixture<TConfiguration, TDocument> : IAsyncLifetime
		where TConfiguration : IMongoConfiguration<TDocument>, new()
	{
		private const string MongoDbImage = "mongo:4.4.5";

		private readonly TConfiguration _configuration;

		private readonly MongoDbContainer _container = new MongoDbBuilder().WithImage(MongoDbImage).Build();

		public MongoFixture()
		{
			_configuration = new TConfiguration();
		}

		public IMongoCollection<TDocument> Collection { get; private set; }

		public async Task InitializeAsync()
		{
			await _container.StartAsync();

			var mongoClient = _configuration.GetMongoClient(_container.GetConnectionString());
			Collection = mongoClient.GetDatabase(_configuration.DatabaseName)
				.GetCollection<TDocument>(_configuration.CollectionName);

			await _configuration.InitializeAsync(Collection);
		}

		public async Task DisposeAsync()
		{
			if (Collection != null)
				await _configuration.DisposeAsync(Collection);

			await _container.StopAsync();
			await _container.DisposeAsync();
		}
	}
}
