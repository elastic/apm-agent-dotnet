// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests;

public class SatelliteAssemblyTests
{
	private readonly ITestOutputHelper _output;

	public SatelliteAssemblyTests(ITestOutputHelper output) => _output = output;

	[Fact]
	public async Task CorrectlyReadSatelliteAssemblyMetadata()
	{
		var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
		var apmServer = new MockApmServer(apmLogger, nameof(BasicTests));
		var port = apmServer.FindAvailablePortToListen();
		apmServer.RunInBackground(port);
		var builder = new StringBuilder();

		using (var profiledApplication = new ProfiledApplication("SatelliteAssemblySample"))
		{
			var environmentVariables = new Dictionary<string, string>
			{
				["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
				["ELASTIC_APM_DISABLE_METRICS"] = "*",
				["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout",
				["ELASTIC_APM_PROFILER_LOG"] = "debug",
			};

			profiledApplication.Start(
				"net6.0",
				TimeSpan.FromMinutes(4),
				environmentVariables,
				null,
				line =>
				{
					_output.WriteLine(line.Line);
					builder.Append(line.Line);
				},
				exception =>
				{
					_output.WriteLine($"{exception}");
					builder.Append(exception);
				});
		}

		builder.ToString().Should().Contain("AssemblyLoadFinished: name=FSharp.Core.resources, version=6.0.0.0, culture=de");
		await apmServer.StopAsync();
	}
}
