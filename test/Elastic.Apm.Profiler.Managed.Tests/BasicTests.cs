// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests;

public class BasicTests
{
	private readonly ITestOutputHelper _output;

	public BasicTests(ITestOutputHelper output) => _output = output;

	/// <summary>
	/// Makes sure Agent.Version is suffixed with `p` when the profiler is loaded
	/// </summary>
	[Fact]
	public async Task AgentVersionTest()
	{
		var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
		var apmServer = new MockApmServer(apmLogger, nameof(BasicTests));
		var port = apmServer.FindAvailablePortToListen();
		apmServer.RunInBackground(port);

		using (var profiledApplication = new ProfiledApplication("SqliteSample"))
		{
			var environmentVariables = new Dictionary<string, string>
			{
				["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
				["ELASTIC_APM_DISABLE_METRICS"] = "*",
				["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout",
			};

			profiledApplication.Start(
				"net5.0",
				TimeSpan.FromMinutes(2),
				environmentVariables,
				null,
				line =>
				{
					_output.WriteLine(line.Line);
				},
				exception => _output.WriteLine($"{exception}"));
		}

		// Asserts that Agent.Version ends with `p`, signaling profiler based agent
		apmServer.ReceivedData.Metadata.First().Service.Agent.Version.Should().EndWith("p");

		await apmServer.StopAsync();
	}
}
