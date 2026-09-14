// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Elastic.Apm.Instrumentations.SqlClient;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.SqlClient.Tests
{
	/// <summary>
	/// Unit tests for <see cref="SqlEventListener" /> which drive the SqlClient EventSource payloads directly,
	/// without requiring a SQL Server instance.
	/// </summary>
	public class SqlEventListenerUnitTests : IDisposable
	{
		private const int BeginExecuteEventId = 1;
		private const int EndExecuteEventId = 2;

		// composite state bits written by SqlClient: 1 = success, 2 = failure, 4 = synchronous
		private const int SynchronousSuccess = 1 | 4;
		private const int SynchronousFailure = 2 | 4;

		private readonly ApmAgent _apmAgent;
		private readonly MockPayloadSender _payloadSender;

		public SqlEventListenerUnitTests()
		{
			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new TestAgentComponents(payloadSender: _payloadSender,
				configuration: new MockConfiguration(openTelemetryBridgeEnabled: "false")));
		}

		// BeginExecute payload: int objectId, string dataSource, string database, string commandText
		private static object[] BeginExecute(int objectId, string commandText) => new object[] { objectId, "localhost", "mydb", commandText };

		// EndExecute payload: int objectId, int compositeState, int sqlExceptionNumber
		private static object[] EndExecute(int objectId, int compositeState = SynchronousSuccess, int sqlExceptionNumber = 0) =>
			new object[] { objectId, compositeState, sqlExceptionNumber };

		[Fact]
		public void BeginExecute_Then_EndExecute_ReportsSingleSpan()
		{
			using var listener = new SqlEventListener(_apmAgent);

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", _ =>
			{
				listener.HandleEvent(BeginExecuteEventId, BeginExecute(42, "SELECT getdate()"));
				listener.HandleEvent(EndExecuteEventId, EndExecute(42));
			});

			var span = _payloadSender.Spans.Should().ContainSingle().Which;
			span.Name.Should().Be("SELECT getdate()");
			span.Type.Should().Be(ApiConstants.TypeDb);
			span.Subtype.Should().Be(ApiConstants.SubtypeMssql);
			span.Outcome.Should().Be(Outcome.Success);
			span.Context.Db.Statement.Should().Be("SELECT getdate()");
			span.Context.Db.Instance.Should().Be("mydb");
			span.Duration.Should().BeGreaterThanOrEqualTo(0);
		}

		/// <summary>
		/// A SQL command is an exit span that never has legitimate children. Making it the current span would let the
		/// competing-instrumentation check below mistake our own span for one created by another module.
		/// </summary>
		[Fact]
		public void SqlSpan_IsNotMadeTheCurrentSpan()
		{
			using var listener = new SqlEventListener(_apmAgent);

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", _ =>
			{
				listener.HandleEvent(BeginExecuteEventId, BeginExecute(42, "SELECT getdate()"));
				_apmAgent.Tracer.CurrentSpan.Should().BeNull();
				listener.HandleEvent(EndExecuteEventId, EndExecute(42));
			});

			_payloadSender.Spans.Should().ContainSingle();
		}

		/// <summary>
		/// When the command being executed is already traced by another module, the event listener must not add a nested span
		/// for the same command. This is the case for Entity Framework (Core and 6) and for the profiler's ADO.NET CallTarget
		/// integrations, whose span is the current span while the command executes.
		/// </summary>
		[Theory]
		[InlineData((short)InstrumentationFlag.SqlClient)]
		[InlineData((short)InstrumentationFlag.AdoNet)]
		[InlineData((short)InstrumentationFlag.EfCore)]
		[InlineData((short)InstrumentationFlag.EfClassic)]
		public void BeginExecute_UnderCompetingDbSpan_DoesNotCreateNestedSpan(short instrumentationFlag)
		{
			using var listener = new SqlEventListener(_apmAgent);

			var transaction = (Transaction)_apmAgent.Tracer.StartTransaction("transaction", "type");
			var competingSpan = transaction.StartSpanInternal("SELECT getdate()", ApiConstants.TypeDb, ApiConstants.SubtypeMssql,
				instrumentationFlag: (InstrumentationFlag)instrumentationFlag, isExitSpan: true);

			listener.HandleEvent(BeginExecuteEventId, BeginExecute(42, "SELECT getdate()"));
			listener.HandleEvent(EndExecuteEventId, EndExecute(42));

			competingSpan.End();
			transaction.End();

			_payloadSender.Spans.Should().ContainSingle().Which.Id.Should().Be(competingSpan.Id);
		}

		/// <summary>
		/// Only the modules that trace the very same command compete. Any other current span, including one started by the
		/// application itself, must not stop the command from being traced.
		/// </summary>
		[Theory]
		[InlineData((short)InstrumentationFlag.None)]
		[InlineData((short)InstrumentationFlag.HttpClient)]
		[InlineData((short)InstrumentationFlag.AspNetCore)]
		public void BeginExecute_UnderNonCompetingSpan_CreatesSpan(short instrumentationFlag)
		{
			using var listener = new SqlEventListener(_apmAgent);

			var transaction = (Transaction)_apmAgent.Tracer.StartTransaction("transaction", "type");
			var currentSpan = transaction.StartSpanInternal("SELECT getdate()", ApiConstants.TypeExternal,
				instrumentationFlag: (InstrumentationFlag)instrumentationFlag);

			listener.HandleEvent(BeginExecuteEventId, BeginExecute(42, "SELECT getdate()"));
			listener.HandleEvent(EndExecuteEventId, EndExecute(42));

			currentSpan.End();
			transaction.End();

			_payloadSender.Spans.Should().HaveCount(2);
			_payloadSender.Spans.Should().ContainSingle(s => s.Type == ApiConstants.TypeDb && s.ParentId == currentSpan.Id);
		}

		[Fact]
		public void EventWithoutPayload_IsIgnored()
		{
			using var listener = new SqlEventListener(_apmAgent);

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", _ =>
			{
				var act = () => listener.HandleEvent(BeginExecuteEventId, null);
				act.Should().NotThrow();
			});

			_payloadSender.Spans.Should().BeEmpty();
		}

		[Fact]
		public void EndExecute_WithoutBeginExecute_IsIgnored()
		{
			using var listener = new SqlEventListener(_apmAgent);

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", _ =>
			{
				var act = () => listener.HandleEvent(EndExecuteEventId, EndExecute(42));
				act.Should().NotThrow();
			});

			_payloadSender.Spans.Should().BeEmpty();
		}

		[Fact]
		public void EndExecute_WithSqlException_ReportsFailureAndError()
		{
			using var listener = new SqlEventListener(_apmAgent);

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", _ =>
			{
				listener.HandleEvent(BeginExecuteEventId, BeginExecute(42, "SELECT getdate()"));
				listener.HandleEvent(EndExecuteEventId, EndExecute(42, SynchronousFailure, 547));
			});

			var span = _payloadSender.Spans.Should().ContainSingle().Which;
			span.Outcome.Should().Be(Outcome.Failure);
			_payloadSender.WaitForErrors();
			_payloadSender.Errors.Should().ContainSingle();
		}

		public void Dispose() => _apmAgent.Dispose();
	}
}
