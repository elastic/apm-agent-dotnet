// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using ProcNet;
using ProcNet.Std;
using StackExchange.Redis;
using Xunit;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit.Abstractions;

namespace Elastic.Apm.StackExchange.Redis.Tests
{
	public class ProfilingSessionTests : IDisposable
	{
		private readonly string _containerId;

		public ProfilingSessionTests(ITestOutputHelper outputHelper)
		{
			var args = new StartArguments("docker", "run", "-p", "6379:6379", "-d", "redis");
			var result = Proc.Start(args, TimeSpan.FromSeconds(10));
			var output = string.Join("", result.ConsoleOut.Select(o => o.Line));

			outputHelper.WriteLine($"docker redis exit code: {result.ExitCode}, output: {output}");

			if (!result.Completed || result.ExitCode != 0)
				throw new Exception($"Could not start redis docker image for tests: {output}");

			_containerId = output;
		}

		[DockerFact]
		public async Task Capture_Redis_Commands()
		{
			var connection = await ConnectionMultiplexer.ConnectAsync("localhost");
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
		}

		public void Dispose()
		{
			if (_containerId != null)
			{
				var args = new StartArguments("docker", "rm", "--force", _containerId);
				var result = Proc.Start(args, TimeSpan.FromSeconds(10));
				if (!result.Completed || result.ExitCode != 0)
				{
					var output = string.Join("", result.ConsoleOut.Select(o => o.Line));
					throw new Exception($"Could not delete redis docker image for tests: {output}");
				}
			}
		}
	}
}
