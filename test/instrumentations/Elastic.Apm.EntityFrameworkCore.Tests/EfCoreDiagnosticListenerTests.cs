// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.EntityFrameworkCore.Tests
{
	public class EfCoreDiagnosticListenerTests : IDisposable
	{
		private readonly ApmAgent _apmAgent;
		private readonly DbConnection _connection;
		private readonly FakeDbContext _dbContext;
		private readonly MockPayloadSender _payloadSender;
		private readonly DbContextOptions<FakeDbContext> _options;

		public EfCoreDiagnosticListenerTests(ITestOutputHelper output)
		{
			// default EfCore in-memory isn't relational, this is why we're using sqlite in-memory
			// https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory#inmemory-is-not-a-relational-database
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();

			_options = new DbContextOptionsBuilder<FakeDbContext>()
				.UseSqlite(_connection)
				.Options;

			_dbContext = new FakeDbContext(_options);
			_dbContext.Database.EnsureCreated();

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new AgentComponents(
				payloadSender: _payloadSender,
				configurationReader: new MockConfiguration(
					exitSpanMinDuration: "0",
					centralConfig: "false",
					// Ensure we always capture (and do not throw away) the stack trace
					// when using this configuration
					spanStackTraceMinDurationInMilliseconds: "0"),
				logger: new UnitTestLogger(output, LogLevel.Trace)
			));
			_apmAgent.Subscribe(new EfCoreDiagnosticsSubscriber());
		}

		public void Dispose()
		{
			_connection?.Close();
			_connection?.Dispose();
			_dbContext?.Dispose();
			_apmAgent?.Dispose();
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldCaptureException_WhenCommandFailed()
		{
			await using var context = new FakeDbContext(_options);
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async _ =>
			{
				try
				{
					await context.Database.ExecuteSqlRawAsync("SELECT * FROM FakeTable");
				}
				catch
				{
					// ignore
				}
			});

			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.FirstSpan.Should().NotBeNull();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Failure);

			_payloadSender.Errors.Count.Should().Be(1);
			_payloadSender.FirstError.Exception.Type.Should().Be(typeof(SqliteException).FullName);
			_payloadSender.FirstError.ParentId.Should().Be(_payloadSender.FirstSpan.Id);
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldCaptureSpan_WhenCommandSucceed()
		{
			await using var context = new FakeDbContext(_options);
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async _ =>
			{
				try
				{
					await context.Database.ExecuteSqlRawAsync("SELECT date('now')");
				}
				catch
				{
					// ignore
				}
			});

			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(0);

			_payloadSender.FirstSpan.Should().NotBeNull();
			_payloadSender.FirstSpan.Outcome.Should().Be(Outcome.Success);
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldNotStartSpan_WhenCurrentTransactionIsNull()
		{
			await using var context = new FakeDbContext(_options);
			try
			{
				await context.Database.ExecuteSqlRawAsync("SELECT date('now')");
			}
			catch
			{
				// ignore
			}

			// Assert
			_payloadSender.Spans.Count.Should().Be(0);
			_payloadSender.Errors.Count.Should().Be(0);
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldCaptureCallingMember_WhenCalledInAsyncContext()
		{
			await using var context = new FakeDbContext(_options);

			await _apmAgent.Tracer.CaptureTransaction("transaction", "type",
				async _ => await context.Data.FirstOrDefaultAsync());

			_payloadSender.FirstSpan.StackTrace.Should().NotBeNull();
			_payloadSender.FirstSpan.StackTrace.Should()
				.Contain(n => n.Function.Contains(nameof(EfCoreDiagnosticListener_ShouldCaptureCallingMember_WhenCalledInAsyncContext)));
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldNotCaptureStackTrace_WhenDurationLessThanConfiguredLimit()
		{
			await using var context = new FakeDbContext(_options);

			var apmAgent = new ApmAgent(new AgentComponents(
				payloadSender: _payloadSender,
				configurationReader: new MockConfiguration(
					exitSpanMinDuration: "0",
					centralConfig: "false",
					// used to test that a stack trace captured on start is thrown away when less than configured duration
					spanStackTraceMinDurationInMilliseconds: "1000"),
				logger: new NoopLogger()
			));
			apmAgent.Subscribe(new EfCoreDiagnosticsSubscriber());

			await apmAgent.Tracer.CaptureTransaction("transaction", "type",
				async _ => await context.Data.FirstOrDefaultAsync());

			_payloadSender.FirstSpan.RawStackTrace.Should().BeNull();
		}
	}
}
