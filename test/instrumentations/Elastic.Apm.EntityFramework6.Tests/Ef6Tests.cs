using System;
using System.Data.Entity;
using Elastic.Apm.DatabaseTests.Common;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable AccessToDisposedClosure

namespace Elastic.Apm.EntityFramework6.Tests
{
	public class Ef6Tests : IDisposable, IClassFixture<DatabaseFixture>
	{
		private readonly ApmAgent _apmAgent;
		private readonly string _connectionString;

		private readonly MockPayloadSender _payloadSender;

		public Ef6Tests(ITestOutputHelper testOutputHelper, DatabaseFixture sqlClientListenerFixture)
		{
			_connectionString = sqlClientListenerFixture.ConnectionString;

			_payloadSender = new MockPayloadSender();
			_apmAgent = new ApmAgent(new AgentComponents(
				new LineWriterToLoggerAdaptor(new XunitOutputToLineWriterAdaptor(testOutputHelper)),
				payloadSender: _payloadSender));
		}

		/// <summary>
		/// Registers the EF 6 interceptor and executes a db statement on a transaction.
		/// Makes sure the db spans were captured.
		/// </summary>
		[Fact]
		public void Ef6InterceptorTest()
		{
			const string personName = "FirstPerson";

			// Ef6Interceptor uses the static agent, so we need to configure the static instance :( This could be improved later
			Agent.Setup(new AgentComponents(payloadSender: _payloadSender, configurationReader: _apmAgent.ConfigurationReader,
				logger: _apmAgent.Logger, metricsCollector: new FakeMetricsCollector(),
				centralConfigFetcher: new NoopCentralConfigFetcher(), currentExecutionSegmentsContainer: new CurrentExecutionSegmentsContainer()));

			using var db = new MyContext(_connectionString);
			Agent.Tracer.CaptureTransaction("FirstTransaction", "Test", transaction =>
			{
				db.Database.CreateIfNotExists();
				db.People.Add(new Person { Name = personName });
				db.SaveChanges();
			});

			// Make sure the create table and the insert statements are captured
			_payloadSender.Spans.Should()
				.Contain(n => n.Context != null && n.Context.Db != null &&
					n.Context.Db.Statement.ToLower().Contains("create table") && n.Context.Db.Statement.ToLower().Contains("people"));

			_payloadSender.Spans.Should()
				.Contain(n => n.Context != null && n.Context.Db != null &&
					n.Context.Db.Statement.ToLower().Contains("insert") && n.Context.Db.Statement.ToLower().Contains("people"));

			_payloadSender.Spans.Should().OnlyContain(n => n.Context.Db.Instance == "master");
		}

		public void Dispose() => _apmAgent?.Dispose();
	}

	public class CodeConfig : DbConfiguration
	{
		public CodeConfig()
			=> AddInterceptor(new Ef6Interceptor());
	}

	[DbConfigurationType(typeof(CodeConfig))]
	public class MyContext : DbContext
	{
		public MyContext(string nameOrConnectionString) : base(nameOrConnectionString) { }

		public DbSet<Person> People { get; set; }
	}

	public class Person
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}
}
