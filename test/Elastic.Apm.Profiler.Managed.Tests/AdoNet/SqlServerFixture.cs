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
	[CollectionDefinition("SqlServer")]
	public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
	{
	}

	public class SqlServerFixture : IAsyncLifetime
	{
		private readonly MsSqlTestcontainer _container;

		public SqlServerFixture()
		{
			var containerBuilder = new TestcontainersBuilder<MsSqlTestcontainer>()
				.WithDatabase(new MsSqlTestcontainerConfiguration
				{
					Password = "StrongPassword(!)!!!1"
				});

			_container = containerBuilder.Build();
		}

		public string ConnectionString { get; private set; }

		public async Task InitializeAsync()
		{
			await _container.StartAsync();
			ConnectionString = _container.ConnectionString;
		}

		public async Task DisposeAsync() => await _container.DisposeAsync();
	}
}
