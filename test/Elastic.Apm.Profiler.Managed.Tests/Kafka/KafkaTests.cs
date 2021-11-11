// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests.Kafka
{
	[Collection("Kafka")]
	public class KafkaTests
	{
		private readonly KafkaFixture _fixture;
		private readonly ITestOutputHelper _output;

		public KafkaTests(KafkaFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
			_output = output;
		}

		[DockerTheory]
		[InlineData("net5.0")]
		public async Task CaptureAutoInstrumentedSpans(string targetFramework)
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(CaptureAutoInstrumentedSpans));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			var ignoreTopic = "ignore-topic";
			using (var profiledApplication = new ProfiledApplication("KafkaSample"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["KAFKA_HOST"] = _fixture.BootstrapServers,
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
					["ELASTIC_APM_IGNORE_MESSAGE_QUEUES"] = ignoreTopic
				};

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					line => _output.WriteLine(line.Line),
					exception => _output.WriteLine($"{exception}"));
			}

			// 6 * 10 consume transactions, 14 produce transactions
			var transactions = apmServer.ReceivedData.Transactions;
			transactions.Should().HaveCount(74);

			var consumeTransactions = transactions.Where(t => t.Name.StartsWith("Kafka RECEIVE")).ToList();
			consumeTransactions.Should().HaveCount(60);

			foreach (var consumeTransaction in consumeTransactions)
			{
				var spans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == consumeTransaction.Id);
				spans.Should().HaveCount(1);
				consumeTransaction.Context.Message.Queue.Should().NotBeNull();
				consumeTransaction.Context.Message.Queue.Name.Should().NotBeNullOrEmpty();
				consumeTransaction.ParentId.Should().NotBeNull();
			}

			var produceTransactions = transactions.Where(t => !t.Name.StartsWith("Kafka RECEIVE")).ToList();
			produceTransactions.Should().HaveCount(14);

			foreach (var produceTransaction in produceTransactions)
			{
				var spans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == produceTransaction.Id).ToList();

				if (produceTransaction.Name.Contains("INVALID-TOPIC"))
					spans.Should().HaveCount(1);
				// the produce transaction shouldn't have an auto instrumented publish span as topic is ignored
				else if (produceTransaction.Name.Contains(ignoreTopic))
					spans.Should().BeEmpty();
				else
					spans.Should().HaveCount(10);

				foreach (var span in spans)
				{
					span.Context.Message.Should().NotBeNull();
					span.Context.Message.Queue.Should().NotBeNull();
					span.Context.Message.Queue.Name.Should().NotBeNullOrEmpty();
				}
			}

			await apmServer.StopAsync();
		}
	}
}
