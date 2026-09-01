// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.SqlClient.Tests
{
	/// <summary>
	/// Unit tests for <see cref="SqlClientDiagnosticListener" /> which drive diagnostic events directly,
	/// without requiring a SQL Server instance.
	/// </summary>
	public class SqlClientDiagnosticListenerUnitTests : IDisposable
	{
		private const string ConnectionString = "Data Source=localhost;Initial Catalog=mydb;User Id=user;Password=password;";

		private readonly ApmAgent _apmAgent;
		private readonly MockPayloadSender _payloadSender;

		public SqlClientDiagnosticListenerUnitTests()
		{
			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender));
		}

		private static long Ticks(TimeSpan timeSpan) => (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);

		private static SqlCommand CreateCommand()
		{
			var connection = new SqlConnection(ConnectionString);
			var command = connection.CreateCommand();
			command.CommandText = "SELECT getdate()";
			return command;
		}

		[Fact]
		public void CommandBefore_Then_CommandAfter_EndsSpan_And_RemovesPendingSpan()
		{
			using var listener = new SqlClientDiagnosticListener(_apmAgent);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);
				_apmAgent.Tracer.CurrentSpan.Should().BeNull();

				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandAfter",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(0);
			});

			_payloadSender.WaitForSpans();
			_payloadSender.Spans.Should().ContainSingle();
		}

		[Fact]
		public void CommandAfter_WithoutExtractableCommand_StillRemovesPendingSpan_And_EndsSpan()
		{
			using var listener = new SqlClientDiagnosticListener(_apmAgent);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);

				// the payload carries no usable Command - the entry must still be removed (issue #2787, fix B)
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandAfter",
					new { OperationId = operationId, Command = (object)null }));
				listener.PendingSpanCount.Should().Be(0);
			});

			_payloadSender.WaitForSpans();
			_payloadSender.Spans.Should().ContainSingle();
		}

		[Fact]
		public void CommandBefore_Then_CommandError_EndsSpanAsFailure_And_RemovesPendingSpan()
		{
			using var listener = new SqlClientDiagnosticListener(_apmAgent);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();
			var exception = new InvalidOperationException("command failed");

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);

				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandError",
					new { OperationId = operationId, Command = command, Exception = exception }));
				listener.PendingSpanCount.Should().Be(0);
			});

			_payloadSender.WaitForSpans();
			_payloadSender.Spans.Should().ContainSingle();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Failure);
			_payloadSender.WaitForErrors();
			_payloadSender.Errors.Should().ContainSingle();
		}

		[Fact]
		public void CommandError_WithoutExtractableCommand_StillRemovesPendingSpan_And_EndsSpanAsFailure()
		{
			using var listener = new SqlClientDiagnosticListener(_apmAgent);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);

				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandError",
					new { OperationId = operationId, Command = (object)null, Exception = new InvalidOperationException("command failed") }));
				listener.PendingSpanCount.Should().Be(0);
			});

			_payloadSender.WaitForSpans();
			_payloadSender.Spans.Should().ContainSingle();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Failure);
		}

		[Fact]
		public void CommandTimeout_OfZero_UsesDefaultFiniteMaxAge()
		{
			long now = 0;
			var store = new PendingSpanStore(new NoopLogger(), sweepInterval: TimeSpan.Zero, clock: () => now,
				minimumMaxAge: TimeSpan.Zero);
			using var listener = new SqlClientDiagnosticListener(_apmAgent, store);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();
			command.CommandTimeout = 0;

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);

				now += Ticks(PendingSpanStore.DefaultMaxAge - TimeSpan.FromSeconds(1));
				store.Sweep();
				listener.PendingSpanCount.Should().Be(1);

				now += Ticks(TimeSpan.FromSeconds(2));
				store.Sweep();
				listener.PendingSpanCount.Should().Be(0);
				store.TryRemove(operationId, out _).Should().BeFalse();
			});

			_payloadSender.Spans.Should().BeEmpty();
		}

		[Fact]
		public void OrphanedPendingSpan_IsAbandoned_WhenTransactionEnds()
		{
			using var store = new PendingSpanStore(new NoopLogger(), sweepInterval: TimeSpan.FromDays(1));
			using var listener = new SqlClientDiagnosticListener(_apmAgent, store);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				var transaction = (Transaction)t;
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(1);
			});

			// Transaction completion abandons the orphan immediately — no sweep/age wait required.
			listener.PendingSpanCount.Should().Be(0);
			store.TryRemove(operationId, out _).Should().BeFalse();
			_payloadSender.Spans.Should().BeEmpty();
			_payloadSender.WaitForTransactions();
			_payloadSender.FirstTransaction.SelfDuration.Should().Be(_payloadSender.FirstTransaction.Duration);
		}

		[Fact]
		public void CommandAfter_WhenEndSpanFails_StillAbandonsPendingSpan()
		{
			using var listener = new SqlClientDiagnosticListener(_apmAgent);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				var transaction = (Transaction)t;
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);

				// Connection is null so DbSpanCommon.EndSpan throws before span.End().
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandAfter",
					new { OperationId = operationId, Command = new ConnectionlessDbCommand() }));

				listener.PendingSpanCount.Should().Be(0);
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(0);
			});

			_payloadSender.Spans.Should().BeEmpty();
		}

		[Fact]
		public void CommandTimeout_SetsMaxAgeToFourTimesTimeout()
		{
			long now = 0;
			var store = new PendingSpanStore(new NoopLogger(), sweepInterval: TimeSpan.Zero, clock: () => now,
				minimumMaxAge: TimeSpan.Zero);
			using var listener = new SqlClientDiagnosticListener(_apmAgent, store);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();
			command.CommandTimeout = 5; // max age = 20s

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				var transaction = (Transaction)t;
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(1);

				now += Ticks(TimeSpan.FromSeconds(19));
				store.Sweep();
				listener.PendingSpanCount.Should().Be(1);

				now += Ticks(TimeSpan.FromSeconds(2));
				store.Sweep();
				listener.PendingSpanCount.Should().Be(0);
				store.TryRemove(operationId, out _).Should().BeFalse();
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(0);
			});

			_payloadSender.Spans.Should().BeEmpty();
		}

		[Fact]
		public void CommandBefore_WithoutMatchingCompletionEvent_IsEvicted()
		{
			// Simulates SqlClient cancellation paths that emit WriteCommandBefore without a matching
			// WriteCommandAfter/WriteCommandError.
			long now = 0;
			var store = new PendingSpanStore(new NoopLogger(), sweepInterval: TimeSpan.Zero, clock: () => now);
			using var listener = new SqlClientDiagnosticListener(_apmAgent, store);
			var orphanedOperationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				var transaction = (Transaction)t;
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = orphanedOperationId, Command = command }));
				listener.PendingSpanCount.Should().Be(1);
				_apmAgent.Tracer.CurrentSpan.Should().BeNull();
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(1);

				// Default CommandTimeout (30s) yields a max age of 120s, floored to the store's 10 minute default
				now += Ticks(PendingSpanStore.DefaultMaxAge + TimeSpan.FromMinutes(1));
				store.Sweep();

				listener.PendingSpanCount.Should().Be(0);
				store.TryRemove(orphanedOperationId, out _).Should().BeFalse();
				_apmAgent.Tracer.CurrentSpan.Should().BeNull();
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(0);
			});

			// Evicted spans are dropped rather than reported with a fabricated duration
			_payloadSender.Spans.Should().BeEmpty();
		}

		[Fact]
		public void CommandAfter_ForEvictedEntry_IsIgnored()
		{
			long now = 0;
			var store = new PendingSpanStore(new NoopLogger(), sweepInterval: TimeSpan.Zero, clock: () => now);
			using var listener = new SqlClientDiagnosticListener(_apmAgent, store);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));

				now += Ticks(PendingSpanStore.DefaultMaxAge + TimeSpan.FromMinutes(1));
				store.Sweep();
				store.TryRemove(operationId, out _).Should().BeFalse();

				// a late completion event for the evicted entry must be a no-op
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandAfter",
					new { OperationId = operationId, Command = command }));
				listener.PendingSpanCount.Should().Be(0);
			});

			_payloadSender.Spans.Should().BeEmpty();
		}

		[Fact]
		public void SqlSpan_DoesNotReplaceOrClobberCurrentSpan()
		{
			using var listener = new SqlClientDiagnosticListener(_apmAgent);
			var operationId = Guid.NewGuid();
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", transaction =>
			{
				var parentSpan = transaction.StartSpan("parent", "test");
				_apmAgent.Tracer.CurrentSpan.Should().BeSameAs(parentSpan);

				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = operationId, Command = command }));
				_apmAgent.Tracer.CurrentSpan.Should().BeSameAs(parentSpan);

				var nestedSpan = parentSpan.StartSpan("nested", "test");
				listener.OnNext(new KeyValuePair<string, object>("Microsoft.Data.SqlClient.WriteCommandAfter",
					new { OperationId = operationId, Command = command }));
				_apmAgent.Tracer.CurrentSpan.Should().BeSameAs(nestedSpan);

				nestedSpan.End();
				parentSpan.End();
			});

			_payloadSender.WaitForSpans(count: 3);
			_payloadSender.Spans.Should().HaveCount(3);
		}

#if !NETFRAMEWORK
		[Fact]
		public void SubscriptionDispose_ClearsPendingSpans_AndAbandonsThem()
		{
			var store = new PendingSpanStore(new NoopLogger());
			using var diagnosticListener = new DiagnosticListener("SqlClientDiagnosticListener");
			using var subscription = _apmAgent.Subscribe(new SqlClientDiagnosticSubscriber(store));
			using var command = CreateCommand();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				var transaction = (Transaction)t;
				diagnosticListener.Write("Microsoft.Data.SqlClient.WriteCommandBefore",
					new { OperationId = Guid.NewGuid(), Command = command });
				store.Count.Should().Be(1);
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(1);

				subscription.Dispose();

				store.Count.Should().Be(0);
				_apmAgent.Tracer.CurrentSpan.Should().BeNull();
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(0);
			});

			_payloadSender.Spans.Should().BeEmpty();
		}
#endif

		public void Dispose() => _apmAgent.Dispose();

		/// <summary>
		/// IDbCommand whose Connection is null so <see cref="DbSpanCommon.EndSpan"/> throws.
		/// </summary>
		private sealed class ConnectionlessDbCommand : IDbCommand
		{
			public string CommandText { get; set; } = "SELECT 1";
			public int CommandTimeout { get; set; } = 30;
			public CommandType CommandType { get; set; } = CommandType.Text;
			public IDbConnection Connection { get; set; }
			public IDataParameterCollection Parameters => throw new NotSupportedException();
			public IDbTransaction Transaction { get; set; }
			public UpdateRowSource UpdatedRowSource { get; set; }

			public void Cancel() => throw new NotSupportedException();
			public IDbDataParameter CreateParameter() => throw new NotSupportedException();
			public void Dispose() { }
			public int ExecuteNonQuery() => throw new NotSupportedException();
			public IDataReader ExecuteReader() => throw new NotSupportedException();
			public IDataReader ExecuteReader(CommandBehavior behavior) => throw new NotSupportedException();
			public object ExecuteScalar() => throw new NotSupportedException();
			public void Prepare() => throw new NotSupportedException();
		}
	}
}
