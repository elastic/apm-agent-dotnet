// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Databases;
using DotNet.Testcontainers.Containers.Modules.Databases;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("Postgres")]
	public class PostgresCollection : ICollectionFixture<PostgreSqlFixture>
	{
	}

	public class PostgreSqlFixture : IAsyncLifetime
	{
		private readonly PostgreSqlTestcontainer _container;
		private const string PostgresUserName = "postgres";
		private const string PostgresPassword = "mysecretpassword";
		private const string PostgresDatabase = "db";

		public PostgreSqlFixture()
		{
			var postgresBuilder = new TestcontainersBuilder<PostgreSqlTestcontainer>()
				.WithDatabase(new PostgreSqlTestcontainerConfiguration
				{
					Database = PostgresDatabase,
					Username = PostgresUserName,
					Password = PostgresPassword
				});

			_container = postgresBuilder.Build();
		}

		public async Task InitializeAsync()
		{
			await _container.StartAsync();
			ConnectionString = _container.ConnectionString;
		}

		public async Task DisposeAsync() => await _container.DisposeAsync();

		public string ConnectionString { get; private set; }
	}
}
