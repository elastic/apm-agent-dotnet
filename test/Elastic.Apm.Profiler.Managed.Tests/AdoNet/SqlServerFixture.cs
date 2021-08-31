// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Utilities;
using Oracle.ManagedDataAccess.Client;
using Polly;
using TestContainers.Core.Builders;
using TestContainers.Core.Containers;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("SqlServer")]
	public class SqlServerCollection : ICollectionFixture<SqlServerFixture>
	{
	}

	public class SqlServerContainer : Container
	{
		public const string IMAGE = "mcr.microsoft.com/mssql/server:2017-CU14-ubuntu";

		public const int SQL_PORT = 1433;

		public const string UserName = "sa";
		public const string Password = "StrongPassword(!)!!!1";

		protected override async Task WaitUntilContainerStarted()
		{
			await base.WaitUntilContainerStarted();

			var result = await Policy
				.TimeoutAsync(TimeSpan.FromMinutes(4))
				.WrapAsync(Policy
					.Handle<Exception>()
					.WaitAndRetryForeverAsync(
						iteration => TimeSpan.FromSeconds(10)))
				.ExecuteAndCaptureAsync(async () =>
				{
					using var connection = new SqlConnection(ConnectionString);
					await connection.OpenAsync();
					using var cmd = new SqlCommand("SELECT 1", connection);
					var reader = await cmd.ExecuteScalarAsync();
				});

			if (result.Outcome == OutcomeType.Failure)
				throw new Exception(result.FinalException.Message);
		}

		public string ConnectionString =>
			$"Server={GetDockerHostIpAddress()},{GetMappedPort(SQL_PORT)};Database=master;User Id={UserName};Password={Password};";
	}

	public class SqlServerFixture : IAsyncLifetime
	{
		private readonly SqlServerContainer _container;
		public SqlServerFixture()
		{
			var builder = new GenericContainerBuilder<SqlServerContainer>()
				.Begin()
				.WithImage(SqlServerContainer.IMAGE)
				.WithEnv(
					("SA_PASSWORD", SqlServerContainer.Password),
					("ACCEPT_EULA", "Y"))
				.WithPortBindings((SqlServerContainer.SQL_PORT, LocalPort.GetAvailablePort()));


			_container = builder.Build();
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
