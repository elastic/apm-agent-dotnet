// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Linq;
using Elastic.Apm.DatabaseTests.Common;
using Elastic.Apm.EntityFrameworkCore;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.SqlClient.Tests
{
	/// <summary>
	/// Elastic.Apm.SqlClient and Elastic.Apm.EntityFrameworkCore can be in a competing setup where both would capture the
	/// same database calls causing db spans being captured twice.
	/// This class creates such a setup, and tests if double-capturing of the same db call does not happen.
	/// </summary>
	public class EfCoreWithMsSqlTests : IDisposable, IClassFixture<DatabaseFixture>
	{
		private readonly ApmAgent _apmAgent;
		private readonly string _connectionString;
		private readonly MockPayloadSender _payloadSender;

		public EfCoreWithMsSqlTests(ITestOutputHelper testOutputHelper, DatabaseFixture sqlClientListenerFixture)
		{
			_connectionString = sqlClientListenerFixture.ConnectionString;

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new AgentComponents(
				new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(testOutputHelper)),
				payloadSender: _payloadSender));
			_apmAgent.Subscribe(new SqlClientDiagnosticSubscriber(), new EfCoreDiagnosticsSubscriber());
		}

		/// <summary>
		/// Executes a db query within a transaction while both SqlClient and EFCore capturing is active.
		/// Makes sure that the db call is only captured once - so only 1 of them captures the call, the other one ignores it.
		/// </summary>
		[Fact]
		public void BothEfCoreAndSqlClientCapturingActive()
		{
			var dbContextOptionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
			dbContextOptionsBuilder.UseSqlServer(_connectionString);

			// Initialize outside the transaction
			using var context = new TestDbContext(dbContextOptionsBuilder.Options);
			context.Database.EnsureCreated();
			context.SampleTable.Add(new DbItem { StrField = "abc" });
			context.SaveChanges();

			_apmAgent.Tracer.CaptureTransaction("transaction", "type", transaction =>
			{
				// ReSharper disable once AccessToDisposedClosure
				var firstItemInDb = context.SampleTable.First();
				Debug.WriteLine(firstItemInDb.StrField);
			});

			_payloadSender.SpansOnFirstTransaction.Should().HaveCount(1);
			_payloadSender.FirstSpan.Should().NotBeNull();
			_payloadSender.FirstSpan.Context?.Db?.Should().NotBeNull();
			_payloadSender.FirstSpan.Context?.Db?.Should().NotBeNull();

			_payloadSender.FirstSpan.Context?.Db?.Instance.Should().Be("master");
			_payloadSender.FirstSpan.Context?.Db?.Type.Should().Be("sql");
		}

		public void Dispose() => _apmAgent?.Dispose();
	}

	public class TestDbContext : DbContext
	{
		public TestDbContext(DbContextOptions<TestDbContext> options)
			: base(options) { }

		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public DbSet<DbItem> SampleTable { get; set; }
	}

	public class DbItem
	{
		public int Id { get; set; }
		public string StrField { get; set; }
	}
}
