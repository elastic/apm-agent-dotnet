using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using TestEnvironment.Docker;
using TestEnvironment.Docker.Containers.Mssql;
using Xunit;

namespace Elastic.Apm.SqlClient.Tests
{
	public class SqlClientDiagnosticListenerTests : IDisposable, IAsyncLifetime
	{
		private readonly DockerEnvironment _environment;

		private readonly MockPayloadSender _payloadSender;
		private readonly ApmAgent _apmAgent;

		private const string ContainerName = "mssql";

		private string _connectionString;

		public SqlClientDiagnosticListenerTests()
		{
			// BUILD_ID env variable is passed from the CI, therefore DockerInDocker is enabled.
			_environment = new DockerEnvironmentBuilder()
				.DockerInDocker(Environment.GetEnvironmentVariable("BUILD_ID") != null)
				.AddMssqlContainer(ContainerName, "StrongPassword!!!!1")
				.Build();

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new AgentComponents(payloadSender: _payloadSender));
			_apmAgent.Subscribe(new SqlClientDiagnosticSubscriber());
		}

		public static IEnumerable<object[]> Connections
		{
			get
			{
				yield return new object[] { new Func<string, DbConnection>(connectionString => new SqlConnection(connectionString)) };
				yield return new object[]
				{
					new Func<string, DbConnection>(connectionString => new Microsoft.Data.SqlClient.SqlConnection(connectionString))
				};
			}
		}

		[Theory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldCaptureSpan(Func<string, DbConnection> connectionCreator)
		{
			const string commandText = "SELECT getdate()";

			// Arrange + Act
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async transaction =>
			{
				using (var dbConnection = connectionCreator.Invoke(_connectionString))
				{
					await dbConnection.OpenAsync();
					using (var sqlCommand = dbConnection.CreateCommand())
					{
						sqlCommand.CommandText = commandText;
						using (await sqlCommand.ExecuteReaderAsync())
						{
							// ignore
						}
					}
				}
			});

			// without delay, listener doesn't have time to process stop event
			await Task.Delay(TimeSpan.FromSeconds(5));

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(0);

			var span = _payloadSender.FirstSpan;

			span.Name.Should().Be(commandText);
			span.Type.Should().Be(ApiConstants.TypeDb);
			span.Subtype.Should().Be(ApiConstants.SubtypeMssql);

			span.Context.Db.Should().NotBeNull();
			span.Context.Db.Statement.Should().Be(commandText);
			span.Context.Db.Type.Should().Be(Database.TypeSql);
		}

		[Theory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldCaptureErrorFromSystemSqlClient(Func<string, DbConnection> connectionCreator)
		{
			const string commandText = "SELECT * FROM FakeTable";

			// Arrange + Act
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async transaction =>
			{
				using (var dbConnection = connectionCreator.Invoke(_connectionString))
				{
					await dbConnection.OpenAsync();
					using (var sqlCommand = dbConnection.CreateCommand())
					{
						sqlCommand.CommandText = commandText;
						try
						{
							using (await sqlCommand.ExecuteReaderAsync())
							{
								// ignore
							}
						}
						catch
						{
							// ignore
						}
					}
				}
			});

			// without delay, listener doesn't have time to process stop event
			await Task.Delay(TimeSpan.FromSeconds(5));

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(1);

			var span = _payloadSender.FirstSpan;

			span.Name.Should().Be(commandText);
			span.Type.Should().Be(ApiConstants.TypeDb);
			span.Subtype.Should().Be(ApiConstants.SubtypeMssql);

			span.Context.Db.Should().NotBeNull();
			span.Context.Db.Statement.Should().Be(commandText);
			span.Context.Db.Type.Should().Be(Database.TypeSql);
		}

		public void Dispose()
		{
			_environment?.Dispose();
			_apmAgent?.Dispose();
		}

		public async Task InitializeAsync()
		{
			await _environment.Up();
			var mssql = _environment.GetContainer<MssqlContainer>(ContainerName);
			_connectionString = mssql.GetConnectionString();
		}

		public async Task DisposeAsync() => await _environment.Down();
	}
}
