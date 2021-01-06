// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.DatabaseTests.Common;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.SqlClient.Tests
{
	public class SqlClientListenerTests : IDisposable, IClassFixture<DatabaseFixture>
	{
		private readonly ApmAgent _apmAgent;

		private readonly string _connectionString;
		private readonly string _expectedAddress;

		private readonly MockPayloadSender _payloadSender;
		private readonly ITestOutputHelper _testOutputHelper;

		public SqlClientListenerTests(ITestOutputHelper testOutputHelper, DatabaseFixture sqlClientListenerFixture)
		{
			_connectionString = sqlClientListenerFixture.ConnectionString;

			_expectedAddress = new SqlConnectionStringBuilder(_connectionString).DataSource.Split(',')[0];

			_testOutputHelper = testOutputHelper;

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new TestAgentComponents(
				new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(_testOutputHelper)),
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
#if !NETFRAMEWORK
				yield return new object[]
				{
					"Microsoft.Data.SqlClient",
					new Func<string, DbConnection>(connectionString => new Microsoft.Data.SqlClient.SqlConnection(connectionString))
				};
#endif
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
				using var dbConnection = connectionCreator.Invoke(_connectionString);
				await dbConnection.OpenAsync();
				using var sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = commandText;
				// ReSharper disable once MethodHasAsyncOverload
				using (sqlCommand.ExecuteReader())
				{
					// ignore
				}
			});

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(0);

			_payloadSender.FirstSpan.Should().NotBeNull();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Success);

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
			span.Context.Destination.Address.Should().Be(_expectedAddress);
			span.Context.Destination.Port.Should().NotBeNull();

			span.Context.Destination.Service.Should().NotBeNull();
			span.Context.Destination.Service.Name.Should().Be(ApiConstants.SubtypeMssql);
			span.Context.Destination.Service.Resource.Should().Be(ApiConstants.SubtypeMssql);
			span.Context.Destination.Service.Type.Should().Be(ApiConstants.TypeDb);
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
				using var dbConnection = connectionCreator.Invoke(_connectionString);
				await dbConnection.OpenAsync();
				using var sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = commandText;
				try
				{
					// ReSharper disable once MethodHasAsyncOverload
					using (sqlCommand.ExecuteReader())
					{
						// ignore
					}
				}
				catch
				{
					// ignore
				}
			});

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(1);

			_payloadSender.FirstSpan.Should().NotBeNull();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Failure);

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
			span.Context.Destination.Address.Should().Be(_expectedAddress);
			span.Context.Destination.Port.Should().NotBeNull();

			span.Context.Destination.Service.Should().NotBeNull();
			span.Context.Destination.Service.Name.Should().Be(ApiConstants.SubtypeMssql);
			span.Context.Destination.Service.Resource.Should().Be(ApiConstants.SubtypeMssql);
			span.Context.Destination.Service.Type.Should().Be(ApiConstants.TypeDb);
		}

		public void Dispose() => _apmAgent?.Dispose();
	}
}
