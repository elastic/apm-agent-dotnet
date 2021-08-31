// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities;
using TestContainers.Core.Builders;
using TestContainers.Core.Containers;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("MySql")]
	public class MySqlCollection : ICollectionFixture<MySqlFixture>
	{
	}

	public class MySqlFixture : IAsyncLifetime
	{
		private readonly MySqlContainer _container;
		private const string MySqlPassword = "Password123";
		private const string MySqlDatabaseName = "db";

		public MySqlFixture()
		{
			var builder = new DatabaseContainerBuilder<MySqlContainer>()
				.Begin()
				.WithImage($"{MySqlContainer.IMAGE}:5.7")
				.WithDatabaseName(MySqlDatabaseName)
				.WithEnv(
					("MYSQL_ROOT_PASSWORD", MySqlPassword),
					("MYSQL_DATABASE", MySqlDatabaseName))
				.WithPortBindings((MySqlContainer.MYSQL_PORT, LocalPort.GetAvailablePort()));

			_container = builder.Build();
		}

		public async Task InitializeAsync()
		{
			await _container.Start();
			// TestContainers does not include the db in the connection string...
			ConnectionString = $"{_container.ConnectionString}database={MySqlDatabaseName};";
		}

		public async Task DisposeAsync() => await _container.Stop();

		public string ConnectionString { get; private set; }
	}
}
