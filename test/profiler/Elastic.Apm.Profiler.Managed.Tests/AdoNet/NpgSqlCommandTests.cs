﻿// Licensed to Elasticsearch B.V under
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

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	[Collection("Postgres")]
	public class NpgSqlCommandTests
	{
		private readonly PostgreSqlFixture _fixture;
		private readonly ITestOutputHelper _output;

		public NpgSqlCommandTests(PostgreSqlFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
			_output = output;
		}

		public static IEnumerable<object[]> TestParameters
		{
			get
			{
				// use the version defined in NpgsqlSample
				var npgSqlVersion = "5.0.7";

				// TODO: Add x64/x86 options. macOS and Linux do not support x86
				yield return new object[] { "net7.0", npgSqlVersion };

				if (TestEnvironment.IsWindows)
					yield return new object[] { "net462", npgSqlVersion };

				npgSqlVersion = "6.0.2";

				yield return new object[] { "net7.0", npgSqlVersion };
			}
		}

		[DockerTheory]
		[MemberData(nameof(TestParameters))]
		public async Task CaptureAutoInstrumentedSpans(string targetFramework, string npgsqlVersion)
		{
			var apmLogger = new UnitTestLogger(_output, Elastic.Apm.Logging.LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(CaptureAutoInstrumentedSpans));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var profiledApplication = new ProfiledApplication("NpgsqlSample"))
			{
				var environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["POSTGRES_CONNECTION_STRING"] = _fixture.ConnectionString,
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
					["ELASTIC_APM_EXIT_SPAN_MIN_DURATION"] = "0",
					["ELASTIC_APM_SPAN_COMPRESSION_ENABLED"] = "false"
				};

				var msBuildProperties = npgsqlVersion is null
					? null
					: new Dictionary<string, string> { ["NpgsqlVersion"] = npgsqlVersion };

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(4),
					environmentVariables,
					msBuildProperties,
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
