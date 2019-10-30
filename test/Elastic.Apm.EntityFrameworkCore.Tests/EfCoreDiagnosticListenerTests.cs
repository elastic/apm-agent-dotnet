using System;
using System.Data.Common;
using System.Threading.Tasks;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Elastic.Apm.EntityFrameworkCore.Tests
{
	public class EfCoreDiagnosticListenerTests : IDisposable
	{
		private readonly ApmAgent _apmAgent;
		private readonly DbConnection _connection;
		private readonly FakeDbContext _dbContext;
		private readonly MockPayloadSender _payloadSender;

		public EfCoreDiagnosticListenerTests()
		{
			// default EfCore in-memory isn't relational, this is why we're using sqlite in-memory
			// https://docs.microsoft.com/en-us/ef/core/miscellaneous/testing/in-memory#inmemory-is-not-a-relational-database
			_connection = new SqliteConnection("DataSource=:memory:");
			_connection.Open();

			var options = new DbContextOptionsBuilder<FakeDbContext>()
				.UseSqlite(_connection)
				.Options;

			_dbContext = new FakeDbContext(options);

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new AgentComponents(payloadSender: _payloadSender));
			_apmAgent.Subscribe(new EfCoreDiagnosticsSubscriber());
		}

		public void Dispose()
		{
			_connection?.Close();
			_dbContext?.Dispose();
			_apmAgent?.Dispose();
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldCaptureException_WhenCommandFailed()
		{
			// Arrange + Act
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async transaction =>
			{
				try
				{
					await _dbContext.Database.ExecuteSqlCommandAsync("SELECT * FROM FakeTable");
				}
				catch
				{
					// ignore
				}
			});

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);

			_payloadSender.Errors.Count.Should().Be(1);
			_payloadSender.FirstError.Exception.Type.Should().Be(typeof(SqliteException).FullName);
			_payloadSender.FirstError.ParentId.Should().Be(_payloadSender.FirstSpan.Id);
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldCaptureSpan_WhenCommandSucceed()
		{
			// Arrange + Act
			await _apmAgent.Tracer.CaptureTransaction("transaction", "type", async transaction =>
			{
				try
				{
					await _dbContext.Database.ExecuteSqlCommandAsync("SELECT date('now')");
				}
				catch
				{
					// ignore
				}
			});

			// Assert
			_payloadSender.Spans.Count.Should().Be(1);
			_payloadSender.Errors.Count.Should().Be(0);
		}

		[Fact]
		public async Task EfCoreDiagnosticListener_ShouldNotStartSpan_WhenCurrentTransactionIsNull()
		{
			// Arrange + Act
			try
			{
				await _dbContext.Database.ExecuteSqlCommandAsync("SELECT date('now')");
			}
			catch
			{
				// ignore
			}

			// Assert
			_payloadSender.Spans.Count.Should().Be(0);
			_payloadSender.Errors.Count.Should().Be(0);
		}
	}
}
