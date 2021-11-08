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
	[Collection("SqlServer")]
	public class SqlCommandTests
	{
		private readonly SqlServerFixture _fixture;
		private readonly ITestOutputHelper _output;

		public SqlCommandTests(SqlServerFixture fixture, ITestOutputHelper output)
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

			using (var profiledApplication = new ProfiledApplication("SqlClientSample"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
					["ELASTIC_APM_SERVICE_NAME"] = $"SqlClientSample-{targetFramework}",
					["SQLSERVER_CONNECTION_STRING"] = _fixture.ConnectionString,
				};

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					line => _output.WriteLine(line.Line),
					exception => _output.WriteLine($"{exception}"));
			}

			apmServer.ReceivedData.Transactions.Should().HaveCount(2);
			apmServer.ReceivedData.Spans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedTotalSpans);

			var genericTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunAllAsync<TDbCommand>");
			genericTransaction.Should().NotBeNull();

			var genericSpans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == genericTransaction.Id).ToList();
			genericSpans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedRunAllAsyncSpans);

			var baseTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunBaseTypesAsync");
			baseTransaction.Should().NotBeNull();

			var baseSpans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == baseTransaction.Id).ToList();
			baseSpans.Should().HaveCount(AdoNetTestData.DbRunnerExpectedRunBaseTypesAsyncSpans);

			await apmServer.StopAsync();
		}
	}
}
