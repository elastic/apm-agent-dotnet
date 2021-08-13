// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using TestContainers.Core.Builders;
using TestContainers.Core.Containers;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("Postgres")]
	public class PostgresCollection : ICollectionFixture<PostgreSqlFixture>
	{
	}

	public class PostgreSqlFixture : IAsyncLifetime
	{
		private readonly PostgreSqlContainer _container;
		private const string PostgresUserName = "postgres";
		private const string PostgresPassword = "mysecretpassword";

		public PostgreSqlFixture()
		{
			var postgresBuilder = new DatabaseContainerBuilder<PostgreSqlContainer>()
				.Begin()
				.WithImage(PostgreSqlContainer.IMAGE)
				.WithUserName(PostgresUserName)
				.WithPassword(PostgresPassword)
				.WithEnv(("POSTGRES_PASSWORD", PostgresPassword))
				.WithExposedPorts(PostgreSqlContainer.POSTGRESQL_PORT);

			_container = postgresBuilder.Build();
		}

		public async Task InitializeAsync()
		{
			await _container.Start();
			ConnectionString = _container.ConnectionString;
		}

		public async Task DisposeAsync() => await _container.Stop();

		public string ConnectionString { get; private set; }
	}
}
