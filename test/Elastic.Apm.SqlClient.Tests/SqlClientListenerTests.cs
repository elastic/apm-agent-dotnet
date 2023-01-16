// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.SqlClient.Tests
{
	[Collection("SqlServer")]
	public class SqlClientListenerTests : IDisposable
	{
		private readonly ApmAgent _apmAgent;

		private readonly string _connectionString;
		private readonly string _expectedAddress;

		private readonly MockPayloadSender _payloadSender;
		private readonly ITestOutputHelper _testOutputHelper;
		private readonly IDisposable _subscription;

		public SqlClientListenerTests(ITestOutputHelper testOutputHelper, SqlServerFixture sqlClientListenerFixture)
		{
			_connectionString = sqlClientListenerFixture.ConnectionString;

			_expectedAddress = new SqlConnectionStringBuilder(_connectionString).DataSource.Split(',')[0];

			_testOutputHelper = testOutputHelper;

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new TestAgentComponents(
				new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(_testOutputHelper)),
				payloadSender: _payloadSender));
			_subscription = _apmAgent.Subscribe(new SqlClientDiagnosticSubscriber());
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
			_payloadSender.WaitForSpans();
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(0);

			_payloadSender.FirstSpan.Should().NotBeNull();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Success);

			var span = _payloadSender.FirstSpan;

#if !NETFRAMEWORK
			span.Name.ToLower().Should().Be("select");
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
			span.Context.Destination.Service.Resource.Should().Be($"{ApiConstants.SubtypeMssql}/{span.Context.Db.Instance}");
			span.Context.Service.Target.Type.Should().Be(ApiConstants.SubtypeMssql);
			span.Context.Service.Target.Name.Should().Be(span.Context.Db.Instance);
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
			_payloadSender.WaitForSpans();
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(1);

			_payloadSender.FirstSpan.Should().NotBeNull();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Failure);

			var span = _payloadSender.FirstSpan;

#if !NETFRAMEWORK
			span.Name.ToLower().Should().Be("select from faketable");
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

			span.Context.Destination.Service.Resource.Should().Be($"{ApiConstants.SubtypeMssql}/{span.Context.Db.Instance}");
			span.Context.Service.Target.Type.Should().Be(ApiConstants.SubtypeMssql);
			span.Context.Service.Target.Name.Should().Be(span.Context.Db.Instance);
		}

		[Theory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldNotUseCumulativeDurations(string providerName, Func<string, DbConnection> connectionCreator)
		{
			const string commandText = "SELECT getdate(); WAITFOR DELAY '00:00:00.010';";

			// Arrange + Act
			_testOutputHelper.WriteLine(providerName);

			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async transaction =>
			{
				using var dbConnection = connectionCreator.Invoke(_connectionString);
				await dbConnection.OpenAsync();

				for (var i = 0; i < 100; i++)
				{
					using var sqlCommand = dbConnection.CreateCommand();
					sqlCommand.CommandText = commandText;

					// ReSharper disable once MethodHasAsyncOverload
					using (sqlCommand.ExecuteReader())
					{
						// ignore
					}
				}
			});

			// Assert
			_payloadSender.WaitForSpans();
			_payloadSender.Spans.Count.Should().Be(100);
			_payloadSender.Errors.Count.Should().Be(0);

			// Cumulative would mean the last span takes 100 * 10ms = 1000ms
			_payloadSender.Spans.Last().Duration.Should().BeLessThan(1000);
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_apmAgent.Dispose();
		}
	}
}
