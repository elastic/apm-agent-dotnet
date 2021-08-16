// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using Polly;
using TestContainers.Core.Builders;
using TestContainers.Core.Containers;
using Xunit;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[CollectionDefinition("Oracle")]
	public class OracleCollection : ICollectionFixture<OracleSqlFixture>
	{
	}

	public class OracleSqlContainer : Container
	{
		public const string IMAGE = "store/oracle/database-enterprise:12.2.0.1";

		public const int ORACLE_PORT = 1521;

		public const string UserName = "system";
		public const string Password = "Oradoc_db1";

		protected override async Task WaitUntilContainerStarted()
		{
			await base.WaitUntilContainerStarted();
			var connection = new OracleConnection(ConnectionString);
			var result = await Policy
				.TimeoutAsync(TimeSpan.FromMinutes(4))
				.WrapAsync(Policy
					.Handle<Exception>()
					.WaitAndRetryForeverAsync(
						iteration => TimeSpan.FromSeconds(10)))
				.ExecuteAndCaptureAsync(async () =>
				{
					await connection.OpenAsync();
					using var cmd = new OracleCommand("SELECT 1 FROM DUAL", connection);
					var reader = await cmd.ExecuteScalarAsync();
				});

			if (result.Outcome == OutcomeType.Failure)
			{
				connection.Dispose();
				throw new Exception(result.FinalException.Message);
			}
		}

		public string ConnectionString =>
			$"USER ID={UserName};PASSWORD={Password};DATA SOURCE=\"(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={GetDockerHostIpAddress()})(PORT={GetMappedPort(ORACLE_PORT)}))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=ORCLCDB.localdomain)))\"";
	}

	public class OracleSqlFixture : IAsyncLifetime
	{
		private readonly OracleSqlContainer _container;
		public OracleSqlFixture()
		{
			var builder = new GenericContainerBuilder<OracleSqlContainer>()
				.Begin()
				.WithImage(OracleSqlContainer.IMAGE)
				.WithExposedPorts(OracleSqlContainer.ORACLE_PORT);

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
