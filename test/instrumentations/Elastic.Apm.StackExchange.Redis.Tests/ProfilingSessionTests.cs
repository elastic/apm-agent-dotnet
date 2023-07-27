// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using FluentAssertions;
using StackExchange.Redis;
using Testcontainers.Redis;
using Xunit;

namespace Elastic.Apm.StackExchange.Redis.Tests
{
	public class ProfilingSessionTests
	{
		[DockerFact]
		public async Task Capture_Redis_Commands_On_Transaction()
		{
			await using var container = new RedisBuilder().Build();
			await container.StartAsync();

			var connection = await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString());
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

			foreach (var span in payloadSender.Spans)
				AssertSpan(span);

			await container.StopAsync();
		}

		[DockerFact]
		public async Task Capture_Redis_Commands_On_Span()
		{
			await using var container = new RedisBuilder().Build();
			await container.StartAsync();

			var connection = await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString());
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

				foreach (var span in transactionSpans.Where(s => !s.Name.StartsWith("parent", StringComparison.Ordinal)))
					AssertSpan(span);
			}

			await container.StopAsync();
		}

		private static void AssertSpan(ISpan span)
		{
			span.Type.Should().Be(ApiConstants.TypeDb);
			span.Name.Should().MatchRegex(@"[G|S]ET");
			span.Subtype.Should().Be(ApiConstants.SubTypeRedis);
			span.Action.Should().Be(ApiConstants.ActionQuery);

			span.Context.Db.Should().NotBeNull();
			span.Context.Db.Type.Should().Be(ApiConstants.SubTypeRedis);
			span.Context.Db.Instance.Should().BeNull();
			span.Context.Db.Statement.Should().BeNull();

			span.Context.Destination.Should().NotBeNull();
			span.Context.Destination.Address.Should().NotBeNullOrEmpty();
			span.Context.Destination.Port.Should().BeGreaterThan(0).And.BeLessThan(65536);

			span.Context.Service.Target.Should().NotBeNull();
			span.Context.Service.Target.Type.Should().Be(ApiConstants.SubTypeRedis);
			span.Context.Service.Target.Name.Should().BeNull();
		}
	}
}
