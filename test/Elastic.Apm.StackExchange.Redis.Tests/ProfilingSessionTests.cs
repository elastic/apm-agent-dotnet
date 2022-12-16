// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using StackExchange.Redis;
using FluentAssertions;

namespace Elastic.Apm.StackExchange.Redis.Tests
{
	public class ProfilingSessionTests
	{
		[DockerFact]
		public async Task Capture_Redis_Commands_On_Transaction()
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

		[DockerFact]
		public async Task Capture_Redis_Commands_On_Span()
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

			var database = connection.GetDatabase();
			for (var i = 0; i < transactionCount; i++)
			{
				await agent.Tracer.CaptureTransaction($"transaction {i}", ApiConstants.TypeDb, async t =>
				{
					// span 1
					await database.StringSetAsync($"string{i}", i);

					// span 2
					await database.StringGetAsync($"string{i}");

					// span 3
					await t.CaptureSpan($"parent span {i}", ApiConstants.TypeDb, async () =>
					{
						// spans 4,5,6,7
						await database.StringSetAsync($"string{i}", i);
						await database.StringGetAsync($"string{i}");
						await database.StringSetAsync($"string{i}", i);
						await database.StringGetAsync($"string{i}");
					});
				});
			}

			var transactions = payloadSender.Transactions;
			transactions.Should().HaveCount(transactionCount);

			var spansPerParentSpan = 4;
			var topLevelSpans = 3;
			var spansPerTransaction = spansPerParentSpan + topLevelSpans;

			payloadSender.Spans.Should().HaveCount(transactionCount * spansPerTransaction);

			foreach (var transaction in transactions)
			{
				var transactionSpans = payloadSender.Spans
					.Where(s => s.TransactionId == transaction.Id)
					.ToList();

				transactionSpans.Should().HaveCount(spansPerTransaction);

				var parentSpans = transactionSpans.Where(s => s.ParentId == s.TransactionId).ToList();
				parentSpans.Should().HaveCount(topLevelSpans);

				var parentSpanId = parentSpans.OrderByDescending(s => s.Timestamp).First().Id;

				var spansOfParentSpan = transactionSpans.Where(s => s.ParentId == parentSpanId).ToList();

				spansOfParentSpan.Should().HaveCount(spansPerParentSpan);
				foreach (var span in spansOfParentSpan)
					span.Context?.Db?.Statement.Should().MatchRegex(@"[G|S]ET string\d");
			}

			await container.StopAsync();
		}
	}
}
