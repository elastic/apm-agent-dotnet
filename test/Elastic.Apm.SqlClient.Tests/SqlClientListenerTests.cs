using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using TestEnvironment.Docker;
using TestEnvironment.Docker.Containers.Mssql;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.SqlClient.Tests
{
	public class SqlClientListenerTests : IDisposable, IAsyncLifetime
	{
		private readonly ITestOutputHelper _testOutputHelper;
		private readonly DockerEnvironment _environment;

		private readonly MockPayloadSender _payloadSender;
		private readonly ApmAgent _apmAgent;

		private const string ContainerName = "mssql";

		private string _connectionString;

		public SqlClientListenerTests(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
			// BUILD_ID env variable is passed from the CI, therefore DockerInDocker is enabled.
			_environment = new DockerEnvironmentBuilder()
				.DockerInDocker(Environment.GetEnvironmentVariable("BUILD_ID") != null)
				.AddMssqlContainer(ContainerName, "StrongPassword!!!!1")
				.Build();

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new AgentComponents(
				logger: new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(_testOutputHelper)),
				payloadSender: _payloadSender));
			_apmAgent.Subscribe(new SqlClientDiagnosticSubscriber());
		}

		public static IEnumerable<object[]> Connections
		{
			get
			{
				yield return new object[]
				{
					"System.Data.SqlClient", new Func<string, DbConnection>(connectionString => new SqlConnection(connectionString))
				};
				yield return new object[]
				{
					"Microsoft.Data.SqlClient",
					new Func<string, DbConnection>(connectionString => new Microsoft.Data.SqlClient.SqlConnection(connectionString))
				};
			}
		}

		[Theory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldCaptureSpan(string providerName, Func<string, DbConnection> connectionCreator)
		{
			const string commandText = "SELECT getdate()";

			// Arrange + Act
			_testOutputHelper.WriteLine(providerName);

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
#endif
			span.Subtype.Should().Be(ApiConstants.SubtypeMssql);
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
		public async Task SqlClientDiagnosticListener_ShouldCaptureErrorFromSystemSqlClient(string providerName,
			Func<string, DbConnection> connectionCreator
		)
		{
			const string commandText = "SELECT * FROM FakeTable";

			// Arrange + Act
			_testOutputHelper.WriteLine(providerName);

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
#endif
			span.Subtype.Should().Be(ApiConstants.SubtypeMssql);
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
