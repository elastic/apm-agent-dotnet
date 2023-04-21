// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Testcontainers.PostgreSql;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("Postgres")]
	public sealed class PostgresCollection : ICollectionFixture<PostgreSqlFixture> { }

	public sealed class PostgreSqlFixture : IAsyncLifetime
	{
		private readonly PostgreSqlContainer _container = new PostgreSqlBuilder().Build();

		public string ConnectionString => _container.GetConnectionString();

		public Task InitializeAsync()
		{
			return _container.StartAsync();
		}

		public Task DisposeAsync()
		{
			return _container.DisposeAsync().AsTask();
		}
	}
}
