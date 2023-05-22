// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Testcontainers.MySql;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("MySql")]
	public sealed class MySqlCollection : ICollectionFixture<MySqlFixture> { }

	public sealed class MySqlFixture : IAsyncLifetime
	{
		private readonly MySqlContainer _container = new MySqlBuilder().WithImage("mysql:8.0.32").Build();

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
