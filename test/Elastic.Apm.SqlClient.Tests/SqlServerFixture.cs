// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Elastic.Apm.SqlClient.Tests
{
	// ReSharper disable once ClassNeverInstantiated.Global - it's used as a generic parameter
	public sealed class SqlServerFixture : IAsyncLifetime
	{
		private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

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
