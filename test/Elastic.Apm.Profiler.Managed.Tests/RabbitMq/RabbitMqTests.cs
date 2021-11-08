// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Profiler.Managed.Tests.Kafka;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests.RabbitMq
{
	[Collection("RabbitMq")]
	public class RabbitMqTests
	{
		private readonly RabbitMqFixture _fixture;
		private readonly ITestOutputHelper _output;

		public RabbitMqTests(RabbitMqFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
			_output = output;
		}

		[DockerTheory]
		[InlineData("net5.0")]
		public async Task CaptureAutoInstrumentedSpans(string targetFramework)
		{
			var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(CaptureAutoInstrumentedSpans));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var profiledApplication = new ProfiledApplication("RabbitMqSample"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["RABBITMQ_HOST"] = _fixture.ConnectionString,
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
					["ELASTIC_APM_IGNORE_MESSAGE_QUEUES"] = "test-ignore-exchange-name,test-ignore-queue-name"
				};

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					line => _output.WriteLine(line.Line),
					exception => _output.WriteLine($"{exception}"));
			}

			var transactions = apmServer.ReceivedData.Transactions;
			var spans = apmServer.ReceivedData.Spans;

			transactions.Should().HaveCount(9);

			var ignoreTransaction = transactions.Single(t => t.Name == "PublishAndGetIgnore");
			// don't capture any spans for ignored queues and messages
			spans.Where(s => s.TransactionId == ignoreTransaction.Id).Should().BeEmpty();

			var publishAndGetTransaction = transactions.Single(t => t.Name == "PublishAndGet");
			spans.Where(s => s.TransactionId == publishAndGetTransaction.Id).Should().HaveCount(3);

			var publishAndGetDefaultTransaction = transactions.Single(t => t.Name == "PublishAndGetDefault");
			spans.Where(s => s.TransactionId == publishAndGetDefaultTransaction.Id).Should().HaveCount(3);

			var senderTransactions = transactions.Where(t => t.Name == "PublishToConsumer").ToList();
			senderTransactions.Should().HaveCount(3);

			var consumeTransactions = transactions.Where(t => t.Name.StartsWith("RabbitMQ RECEIVE from")).ToList();
			consumeTransactions.Should().HaveCount(3);

			foreach (var senderTransaction in senderTransactions)
			{
				var senderSpan = spans.FirstOrDefault(s => s.TransactionId == senderTransaction.Id);
				senderSpan.Should().NotBeNull();

				var tracingTransaction = consumeTransactions.FirstOrDefault(t => t.TraceId == senderTransaction.TraceId);
				tracingTransaction.Should().NotBeNull();
				tracingTransaction.ParentId.Should().Be(senderSpan.Id);
			}

			foreach (var consumeTransaction in consumeTransactions)
				spans.Where(s => s.TransactionId == consumeTransaction.Id).Should().HaveCount(1);

			await apmServer.StopAsync();
		}
	}
}
