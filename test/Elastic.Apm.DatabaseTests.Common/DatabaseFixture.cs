using System;
using System.Threading.Tasks;
using TestEnvironment.Docker;
using TestEnvironment.Docker.Containers.Mssql;
using Xunit;

namespace Elastic.Apm.DatabaseTests.Common
{
	public class DatabaseFixture : IDisposable, IAsyncLifetime
	{
		private const string ContainerName = "mssql";
		private readonly DockerEnvironment _environment;

		public DatabaseFixture() =>
			// BUILD_ID env variable is passed from the CI, therefore DockerInDocker is enabled.
			_environment = new DockerEnvironmentBuilder()
				.DockerInDocker(Environment.GetEnvironmentVariable("BUILD_ID") != null)
				.AddMssqlContainer(ContainerName, "StrongPassword!!!!1")
				.Build();

		public string ConnectionString { get; private set; }

		public void Dispose() => _environment?.Dispose();

		public async Task InitializeAsync()
		{
			await _environment.Up();
			var mssql = _environment.GetContainer<MssqlContainer>(ContainerName);
			ConnectionString = mssql.GetConnectionString();
		}

		public async Task DisposeAsync() => await _environment.Down();
	}
}
