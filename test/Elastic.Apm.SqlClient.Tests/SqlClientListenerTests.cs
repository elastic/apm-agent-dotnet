using System;
using System.Collections.Generic;
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
	public class SqlClientListenerTests : IDisposable, IAsyncLifetime
	{
		private readonly DockerEnvironment _environment;

		private readonly MockPayloadSender _payloadSender;
		private readonly ApmAgent _apmAgent;

		private const string ContainerName = "mssql";

		private string _connectionString;

		public SqlClientListenerTests()
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
#if !NETFRAMEWORK
				yield return new object[]
				{
					new Func<string, DbConnection>(connectionString => new Microsoft.Data.SqlClient.SqlConnection(connectionString))
				};
#endif
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
						using (sqlCommand.ExecuteReader())
						{
							// ignore
						}
					}
				}
			});

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(0);

			var span = _payloadSender.FirstSpan;

#if !NETFRAMEWORK
			span.Name.Should().Be(commandText);
			span.Subtype.Should().Be(ApiConstants.SubtypeMssql);
#endif
			span.Type.Should().Be(ApiConstants.TypeDb);

			span.Context.Db.Should().NotBeNull();
#if !NETFRAMEWORK
			span.Context.Db.Statement.Should().Be(commandText);
#endif
			span.Context.Db.Type.Should().Be(Database.TypeSql);

			span.Context.Destination.Should().NotBeNull();
			span.Context.Destination.Address.Should().Be("localhost");
			span.Context.Destination.Port.Should().NotBeNull();
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
							using (sqlCommand.ExecuteReader())
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

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(1);

			var span = _payloadSender.FirstSpan;

#if !NETFRAMEWORK
			span.Name.Should().Be(commandText);
			span.Subtype.Should().Be(ApiConstants.SubtypeMssql);
#endif
			span.Type.Should().Be(ApiConstants.TypeDb);

			span.Context.Db.Should().NotBeNull();
#if !NETFRAMEWORK
			span.Context.Db.Statement.Should().Be(commandText);
#endif
			span.Context.Db.Type.Should().Be(Database.TypeSql);

			span.Context.Destination.Should().NotBeNull();
			span.Context.Destination.Address.Should().Be("localhost");
			span.Context.Destination.Port.Should().NotBeNull();
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
