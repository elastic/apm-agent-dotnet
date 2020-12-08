// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Containers.Builders;
using DotNet.Testcontainers.Containers.Configurations.Databases;
using DotNet.Testcontainers.Containers.Modules.Databases;
using Elastic.Apm.Api;
using StackExchange.Redis;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;

namespace Elastic.Apm.StackExchange.Redis.Tests
{
	public class ProfilingSessionTests
	{
		[DockerFact]
		public async Task Capture_Redis_Commands()
		{
			var containerBuilder = new TestcontainersBuilder<RedisTestcontainer>()
				.WithDatabase(new RedisTestcontainerConfiguration());

			await using var container = containerBuilder.Build();
			await container.StartAsync();

			var connection = await ConnectionMultiplexer.ConnectAsync(container.ConnectionString);
			var count = 0;

			while (!connection.IsConnected)
			{
				if (count < 5)
				{
					count++;
					await Task.Delay(500);
				}
				else
					throw new Exception("Could not connect to redis for integration test");
			}

			var payloadSender = new MockPayloadSender();
			var transactionCount = 2;

			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			connection.UseElasticApm(agent);

			for (var i = 0; i < transactionCount; i++)
			{
				await agent.Tracer.CaptureTransaction("Set and Get String", ApiConstants.TypeDb, async () =>
				{
					var database = connection.GetDatabase();
					await database.StringSetAsync($"string{i}", i);
					await database.StringGetAsync($"string{i}");
					await database.StringSetAsync($"string{i}", i);
					await database.StringGetAsync($"string{i}");

					// fire and forget commands may not end up being captured before transaction end is
					// called and profiling session is finished
					await database.StringSetAsync($"string{i}", i, flags: CommandFlags.FireAndForget);
					await database.StringGetAsync($"string{i}", CommandFlags.FireAndForget);
				});
			}

			var transactions = payloadSender.Transactions;
			transactions.Should().HaveCount(transactionCount);

			var minSpansPerTransaction = 4;
			payloadSender.Spans.Should().HaveCountGreaterOrEqualTo(transactionCount * minSpansPerTransaction);

			foreach (var transaction in transactions)
				payloadSender.Spans.Count(s => s.TransactionId == transaction.Id).Should().BeGreaterOrEqualTo(minSpansPerTransaction);

			await container.StopAsync();
		}
	}
}
