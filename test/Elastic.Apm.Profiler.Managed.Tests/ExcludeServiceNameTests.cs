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
	public class ExcludeServiceNameTests
	{
		private readonly ITestOutputHelper _output;

		public ExcludeServiceNameTests(ITestOutputHelper output) => _output = output;

		[Fact]
		public async Task ShouldNotInstrumentExcludedServiceName()
		{
			var apmLogger = new InMemoryBlockingLogger(LogLevel.Error);
			var apmServer = new MockApmServer(apmLogger, nameof(ShouldNotInstrumentExcludedServiceName));
			var port = apmServer.FindAvailablePortToListen();
			apmServer.RunInBackground(port);

			var logs = new List<string>();
			var serviceName = "ServiceName";

			using (var profiledApplication = new ProfiledApplication("SqliteSample"))
			{
				var environmentVariables = new Dictionary<string, string>
				{
					["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
					["ELASTIC_APM_DISABLE_METRICS"] = "*",
					["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout",
					["ELASTIC_APM_SERVICE_NAME"] = serviceName,
					["ELASTIC_APM_PROFILER_EXCLUDE_SERVICE_NAMES"] = serviceName
				};

				profiledApplication.Start(
					"net5.0",
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
				line.Contains($"service name {serviceName} matches excluded name {serviceName}. Profiler disabled"));

			// count of manual spans without any auto instrumented spans
			apmServer.ReceivedData.Spans.Should().HaveCount(32);

			await apmServer.StopAsync();
		}
	}
}
