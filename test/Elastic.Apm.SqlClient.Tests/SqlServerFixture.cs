// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Databases;
using DotNet.Testcontainers.Containers.Modules.Databases;
using Xunit;

namespace Elastic.Apm.SqlClient.Tests
{
	public class SqlServerFixture : IDisposable, IAsyncLifetime
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

		public async Task DisposeAsync()
		{
			await _container.StopAsync();
			_container.Dispose();
		}

		public void Dispose() => _container?.Dispose();
	}
}
