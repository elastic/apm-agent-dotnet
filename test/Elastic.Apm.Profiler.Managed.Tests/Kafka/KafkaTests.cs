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

			using (var profiledApplication = new ProfiledApplication("KafkaSample"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["KAFKA_HOST"] = _fixture.BootstrapServers,
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
				};

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					line => _output.WriteLine(line.Line),
					exception => _output.WriteLine($"{exception}"));
			}

			// 6 * 10 consume transactions, 8 produce transactions
			var transactions = apmServer.ReceivedData.Transactions;
			transactions.Should().HaveCount(68);

			var consumeTransactions = transactions.Where(t => t.Name.StartsWith("Kafka RECEIVE")).ToList();
			consumeTransactions.Should().HaveCount(60);

			foreach (var consumeTransaction in consumeTransactions)
			{
				var spans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == consumeTransaction.Id);
				spans.Should().HaveCount(1);
			}

			var produceTransactions = transactions.Where(t => !t.Name.StartsWith("Kafka RECEIVE")).ToList();
			produceTransactions.Should().HaveCount(8);

			foreach (var produceTransaction in produceTransactions)
			{
				var spans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == produceTransaction.Id);
				spans.Should().HaveCount(produceTransaction.Name.Contains("INVALID-TOPIC") ? 1 : 10, produceTransaction.Name);
			}

			await apmServer.StopAsync();
		}
	}
}
