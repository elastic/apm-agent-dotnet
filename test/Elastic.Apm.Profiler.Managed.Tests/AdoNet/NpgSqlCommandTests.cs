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
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests.AdoNet
{
	public class NpgSqlCommandTests : IClassFixture<PostgreSqlFixture>
	{
		private readonly PostgreSqlFixture _fixture;
		private readonly ITestOutputHelper _output;

		public NpgSqlCommandTests(PostgreSqlFixture fixture, ITestOutputHelper output)
		{
			_fixture = fixture;
			_output = output;
		}

		public static IEnumerable<object[]> TargetFrameworks()
		{
			// TODO: Add x64/x86 options. macOS and Linux do not support x86

			yield return new object[] { "net5.0" };
			yield return new object[] { "netcoreapp3.1" };

			// macOS only supports netcoreapp3.1 and up
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				yield return new object[] { "netcoreapp3.0" };
				yield return new object[] { "netcoreapp2.1" };
			}

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				yield return new object[] { "net461" };
		}

		[Theory]
		[MemberData(nameof(TargetFrameworks))]
		public async Task CaptureAutoInstrumentedSpans(string targetFramework)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(CaptureAutoInstrumentedSpans));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			using (var profiledApplication = new ProfiledApplication("NpgsqlSample"))
			{
				IDictionary<string, string> environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_TRANSACTION_MAX_SPANS"] = "-1",
					["POSTGRES_CONNECTION_STRING"] = _fixture.ConnectionString,
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

			// For RunAllAsync<TDbCommand> transaction:
			// NpgsqlCommand command span:
			//   sync span + 7 command spans
			//   async span + 7 command spans
			//   async with cancellation span + 7 command spans
			// 25 spans
			//
			// DbCommand command span:
			//   sync span + 7 command spans
			//   async span + 7 command spans
			//   async with cancellation span + 7 command spans
			// 25 spans
			//
			// IDbCommand command span:
			//   sync span + 7 command spans
			// 9 spans
			//
			// IDbCommandGenericConstraint<NpgsqlCommand> command span:
			//   sync span + 7 command spans
			// 9 spans
			//
			// DbCommand-NetStandard command span:
			//   sync span + 7 command spans
			//   async span + 7 command spans
			//   async with cancellation span + 7 command spans
			// 25 spans
			//
			// IDbCommand-NetStandard command span:
			//   sync span + 7 command spans
			// 9 spans
			//
			// IDbCommandGenericConstraint<NpgsqlCommand>-NetStandard command span:
			//   sync span + 7 command spans
			// 9 spans
			//
			// 111 spans total
			// ----
			// For RunBaseTypesAsync transaction:
			// DbCommand command span:
			//   sync span + 7 command spans
			//   async span + 7 command spans
			//   async with cancellation span + 7 command spans
			// 25 spans
			//
			// IDbCommand command span:
			//   sync span + 7 command spans
			// 9 spans
			//
			// DbCommand-NetStandard command span:
			//   sync span + 7 command spans
			//   async span + 7 command spans
			//   async with cancellation span + 7 command spans
			// 25 spans
			//
			// IDbCommand-NetStandard command span:
			//   sync span + 7 command spans
			// 9 spans
			//
			// 68 spans total
			// ----
			// 179 spans total
			apmServer.ReceivedData.Spans.Should().HaveCount(179);

			var genericTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunAllAsync<TDbCommand>");
			genericTransaction.Should().NotBeNull();

			var genericSpans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == genericTransaction.Id).ToList();
			genericSpans.Should().HaveCount(111);

			var baseTransaction = apmServer.ReceivedData.Transactions.FirstOrDefault(t => t.Name == "RunBaseTypesAsync");
			baseTransaction.Should().NotBeNull();

			var baseSpans = apmServer.ReceivedData.Spans.Where(s => s.TransactionId == baseTransaction.Id).ToList();
			baseSpans.Should().HaveCount(68);

			await apmServer.StopAsync();
		}
	}
}
