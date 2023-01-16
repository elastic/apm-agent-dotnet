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
	[Collection("Oracle")]
	public class OracleManagedDataAccessCommandTests
	{
		private readonly OracleSqlFixture _fixture;
		private readonly ITestOutputHelper _output;

		public OracleManagedDataAccessCommandTests(OracleSqlFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
			_output = output;
		}

		[DockerTheory]
		[InlineData("net461")]
		public async Task CaptureAutoInstrumentedSpans(string targetFramework)
		{
			if (!TestEnvironment.IsWindows)
				return;

			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(CaptureAutoInstrumentedSpans));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var profiledApplication = new ProfiledApplication("OracleManagedDataAccessSample"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ORACLE_CONNECTION_STRING"] = _fixture.ConnectionString,
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
					// to fix ORA-01882 Timezone region not found on CI.
					["TZ"] = "GMT",
					["ELASTIC_APM_EXIT_SPAN_MIN_DURATION"] = "0",
					["ELASTIC_APM_SPAN_COMPRESSION_ENABLED"] = "false"
				};

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					null,
					line => _output.WriteLine(line.Line),
					exception => _output.WriteLine($"{exception}"));
			}

			apmServer.ReceivedData.Transactions.Should().HaveCount(2);
			apmServer.ReceivedData.Spans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedTotalSpans);

			var testSpans = apmServer.ReceivedData.Spans
				.Where(s => !s.Name.StartsWith(AdoNetTestData.OracleProviderSpanNameStart))
				.ToList();

			var genericTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunAllAsync<TDbCommand>");
			genericTransaction.Should().NotBeNull();

			var genericSpans = testSpans.Where(s => s.TransactionId == genericTransaction.Id).ToList();
			genericSpans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedRunAllAsyncSpans);

			var baseTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunBaseTypesAsync");
			baseTransaction.Should().NotBeNull();

			var baseSpans = testSpans.Where(s => s.TransactionId == baseTransaction.Id).ToList();
			baseSpans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedRunBaseTypesAsyncSpans);

			await apmServer.StopAsync();
		}
	}
}
