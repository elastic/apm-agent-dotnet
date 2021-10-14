// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Docker;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[Collection("MySql")]
	public class MySqlCommandTests
	{
		private readonly MySqlFixture _fixture;
		private readonly ITestOutputHelper _output;

		public MySqlCommandTests(MySqlFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
			_output = output;
		}

		[DockerTheory]
		[ClassData(typeof(AdoNetTestData))]
		public async Task CaptureAutoInstrumentedSpans(string targetFramework)
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(CaptureAutoInstrumentedSpans));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var profiledApplication = new ProfiledApplication("MySqlDataSample"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["MYSQL_CONNECTION_STRING"] = _fixture.ConnectionString,
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
				};

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					line => _output.WriteLine(line.Line),
					exception => _output.WriteLine($"{exception}"));
			}

			// RunAllAsync<TDbCommand> transaction
			// RunBaseTypesAsync transaction
			apmServer.ReceivedData.Transactions.Should().HaveCount(2);

			// The first MySqlCommand on an opened MySqlConnection executes an additional
			// command that the profiler instrumentation will create a span for. Since there
			// are two connections opened, 1 for RunAllAsync<TDbCommand> and 1 for RunBaseTypesAsync,
			// expect 2 additional spans
			apmServer.ReceivedData.Spans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedTotalSpans + 2);

			var genericTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunAllAsync<TDbCommand>");
			genericTransaction.Should().NotBeNull();

			var genericSpans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == genericTransaction.Id).ToList();
			genericSpans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedRunAllAsyncSpans + 1);

			var baseTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunBaseTypesAsync");
			baseTransaction.Should().NotBeNull();

			var baseSpans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == baseTransaction.Id).ToList();
			baseSpans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedRunBaseTypesAsyncSpans + 1);

			await apmServer.StopAsync();
		}
	}
}
