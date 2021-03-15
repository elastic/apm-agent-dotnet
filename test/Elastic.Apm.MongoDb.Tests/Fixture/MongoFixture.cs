// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using MongoDB.Driver;
using TestEnvironment.Docker;
using TestEnvironment.Docker.Containers.Mongo;
using Xunit;

namespace Elastic.Apm.MongoDb.Tests.Fixture
{
	public class MongoFixture<TConfiguration, TDocument> : IAsyncLifetime, IDisposable
		where TConfiguration : IMongoConfiguration<TDocument>, new()
	{
		private readonly TConfiguration _configuration;
		private readonly DockerEnvironment _environment;

		public MongoFixture()
		{
			_configuration = new TConfiguration();

			// BUILD_ID env variable is passed from the CI, therefore DockerInDocker is enabled.
			_environment = new DockerEnvironmentBuilder()
				.DockerInDocker(Environment.GetEnvironmentVariable("BUILD_ID") != null)
				.AddMongoContainer("mongo")
				.Build();
		}

		public IMongoCollection<TDocument> Collection { get; private set; }

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
