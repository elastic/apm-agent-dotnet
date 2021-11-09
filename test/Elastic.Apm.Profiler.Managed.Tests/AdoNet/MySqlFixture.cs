// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("MySql")]
	public class MySqlCollection : ICollectionFixture<MySqlFixture>
	{
	}

	public class MySqlFixture : IAsyncLifetime
	{
		private readonly MySqlTestcontainer _container;
		private const string MySqlPassword = "Password123";
		private const string MySqlDatabaseName = "db";
		private const string MySqlUsername = "mysql";

		public MySqlFixture()
		{
			var builder = new TestcontainersBuilder<MySqlTestcontainer>()
				.WithDatabase(new MySqlTestcontainerConfiguration
				{
					Database = MySqlDatabaseName,
					Username = MySqlUsername,
					Password = MySqlPassword
				});

			_container = builder.Build();
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
