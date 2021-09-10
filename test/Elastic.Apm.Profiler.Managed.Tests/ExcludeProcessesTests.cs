// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
using Elastic.Apm.Profiler.Managed.Tests.AdoNet;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests
{
	public class ExcludeProcessesTests
	{
		private readonly ITestOutputHelper _output;

		public ExcludeProcessesTests(ITestOutputHelper output) => _output = output;

		[Theory]
		[InlineData("net5.0", "dotnet.exe")]
		[InlineData("net461", "SqliteSample.exe")]
		public async Task ShouldNotInstrumentExcludedProcess(string targetFramework, string excludeProcess)
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(ShouldNotInstrumentExcludedProcess));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			var logs = new List<string>();

			using (var profiledApplication = new ProfiledApplication("SqliteSample"))
			{
				var environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
					["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout",
					["ELASTIC_APM_PROFILER_EXCLUDE_PROCESSES"] = excludeProcess
				};

				profiledApplication.Start(
					targetFramework,
					TimeSpan.FromMinutes(2),
					environmentVariables,
					line =>
					{
						if (line.Line.StartsWith("["))
							logs.Add(line.Line);
						else
							_output.WriteLine(line.Line);
					},
					exception => _output.WriteLine($"{exception}"));
			}

			logs.Should().Contain(line =>
				line.Contains($"process name {excludeProcess} matches excluded name {excludeProcess}. Profiler disabled"));

			// count of manual spans without any auto instrumented spans
			apmServer.ReceivedData.Spans.Should().HaveCount(32);

			await apmServer.StopAsync();
		}
	}
}
