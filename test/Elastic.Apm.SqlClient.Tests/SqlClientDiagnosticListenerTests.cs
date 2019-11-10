using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
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
			_environment = new DockerEnvironmentBuilder()
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
			// Arrange + Act
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async transaction =>
			{
				using (var dbConnection = connectionCreator.Invoke(_connectionString))
				{
					await dbConnection.OpenAsync();
					using (var sqlCommand = dbConnection.CreateCommand())
					{
						sqlCommand.CommandText = "SELECT getdate()";
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
		}

		[Theory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldCaptureErrorFromSystemSqlClient(Func<string, DbConnection> connectionCreator)
		{
			// Arrange + Act
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async transaction =>
			{
				using (var dbConnection = connectionCreator.Invoke(_connectionString))
				{
					await dbConnection.OpenAsync();
					using (var sqlCommand = dbConnection.CreateCommand())
					{
						sqlCommand.CommandText = "SELECT * FROM FakeTable";
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
