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
	[CollectionDefinition("Oracle")]
	public class OracleCollection : ICollectionFixture<OracleSqlFixture>
	{
	}

	public class OracleSqlFixture : IAsyncLifetime
	{
		private readonly OracleTestcontainer _container;

		public OracleSqlFixture()
		{
			var builder = new TestcontainersBuilder<OracleTestcontainer>()
				.WithDatabase(new OracleTestcontainerConfiguration { Password = "oracle" });

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
