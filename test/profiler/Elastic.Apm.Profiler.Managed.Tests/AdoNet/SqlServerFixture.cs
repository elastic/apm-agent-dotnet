// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("SqlServer")]
	public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }

	public sealed class SqlServerFixture : IAsyncLifetime
	{
		private readonly MsSqlContainer _container;

		public SqlServerFixture()
		{
			// see: https://blog.rufer.be/2024/09/22/workaround-fix-testcontainers-sql-error-docker-dotnet-dockerapiexception-docker-api-responded-with-status-codeconflict/
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				_container = new MsSqlBuilder()
					.WithImage("mcr.microsoft.com/mssql/server:2022-latest")
					.Build();
			}
			else
			{
				_container = new MsSqlBuilder()
					.Build();
			}
		}

		public string ConnectionString => _container.GetConnectionString();

		public Task InitializeAsync() => _container.StartAsync();

		public Task DisposeAsync() => _container.DisposeAsync().AsTask();
	}
}
