// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
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

			var logger = new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(_testOutputHelper));
			_payloadSender = new MockPayloadSender(logger);
			_apmAgent = new ApmAgent(new TestAgentComponents(
				logger,
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

		[DockerTheory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldCaptureSpan(string providerName, Func<string, DbConnection> connectionCreator)
		{
			const string commandText = "SELECT getdate()";

			// Arrange + Act
			_testOutputHelper.WriteLine(providerName);

			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async _ =>
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

		[DockerTheory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldCaptureErrorFromSystemSqlClient(string providerName,
			Func<string, DbConnection> connectionCreator
		)
		{
			const string commandText = "SELECT * FROM FakeTable";

			// Arrange + Act
			_testOutputHelper.WriteLine(providerName);

			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async _ =>
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

		[DockerTheory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldNotUseCumulativeDurations(string providerName, Func<string, DbConnection> connectionCreator)
		{
			const string commandText = "SELECT getdate(); WAITFOR DELAY '00:00:00.010';";

			// Arrange + Act
			_testOutputHelper.WriteLine(providerName);

			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async _ =>
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

#if !NETFRAMEWORK
		[DockerTheory]
		[MemberData(nameof(Connections))]
		public async Task SqlClientDiagnosticListener_ShouldReleasePendingSpanAfterCommandTimeout(string providerName,
			Func<string, DbConnection> connectionCreator
		)
		{
			_testOutputHelper.WriteLine(providerName);
			using var store = new PendingSpanStore(_apmAgent.Logger, sweepInterval: TimeSpan.FromMilliseconds(50),
				minimumMaxAge: TimeSpan.Zero);
			using var listener = new SqlClientDiagnosticListener(_apmAgent, store);
			using var startOnlyObserver = new StartOnlySqlClientObserver(listener);
			using var allListenersSubscription = DiagnosticListener.AllListeners.Subscribe(startOnlyObserver);

			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async _ =>
			{
				using var dbConnection = connectionCreator.Invoke(_connectionString);
				await dbConnection.OpenAsync();
				using var sqlCommand = dbConnection.CreateCommand();
				sqlCommand.CommandText = "WAITFOR DELAY '00:00:10'";
				sqlCommand.CommandTimeout = 1;

				var executionTask = sqlCommand.ExecuteNonQueryAsync();
				SpinWait.SpinUntil(() => store.Count == 1, TimeSpan.FromSeconds(5)).Should().BeTrue();
				_apmAgent.Tracer.CurrentSpan.Should().BeNull();

				await Assert.ThrowsAnyAsync<DbException>(() => executionTask);
				store.Count.Should().Be(1);
			});

			SpinWait.SpinUntil(() => store.Count == 0, TimeSpan.FromSeconds(10)).Should().BeTrue();
		}

		private sealed class StartOnlySqlClientObserver(SqlClientDiagnosticListener listener) : IObserver<DiagnosticListener>, IDisposable
		{
			private readonly SqlClientDiagnosticListener _listener = listener;
			private readonly CompositeDisposable _subscriptions = new();

			public void OnCompleted() { }

			public void OnError(Exception error) { }

			public void OnNext(DiagnosticListener listener)
			{
				if (listener.Name == _listener.Name)
					_subscriptions.Add(listener.Subscribe(new StartOnlyEventObserver(_listener)));
			}

			public void Dispose() => _subscriptions.Dispose();
		}

		private sealed class StartOnlyEventObserver(SqlClientDiagnosticListener listener) : IObserver<KeyValuePair<string, object>>
		{
			private readonly SqlClientDiagnosticListener _listener = listener;

			public void OnCompleted() { }

			public void OnError(Exception error) { }

			public void OnNext(KeyValuePair<string, object> value)
			{
				if (value.Key.EndsWith("WriteCommandBefore", StringComparison.Ordinal))
					_listener.OnNext(value);
			}
		}
#endif

		public void Dispose()
		{
			_subscription.Dispose();
			_apmAgent.Dispose();
		}
	}
}
