// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elastic.Apm.Cloud;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.XUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests;

public class ExcludeTests
{
	private readonly ITestOutputHelper _output;

	public ExcludeTests(ITestOutputHelper output) => _output = output;

	[Fact]
	public async Task ShouldNotInstrumentExcludedIntegrations()
	{
		var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
		var apmServer = new MockApmServer(apmLogger, nameof(ShouldNotInstrumentExcludedIntegrations));
		var port = apmServer.FindAvailablePortToListen();
		apmServer.RunInBackground(port);

		var logs = new List<string>();

		using (var profiledApplication = new ProfiledApplication("SqliteSample"))
		{
			var environmentVariables = new Dictionary<string, string>
			{
				["ELASTIC_APM_SERVER_URL"] = $"http://localhost:{port}",
				["ELASTIC_APM_PROFILER_EXCLUDE_INTEGRATIONS"] = "SqliteCommand;AdoNet",
				["ELASTIC_APM_DISABLE_METRICS"] = "*",
				["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout"
			};

			profiledApplication.Start(
				"net5.0",
				TimeSpan.FromMinutes(2),
				environmentVariables,
				null,
				line =>
				{
					if (line.Line.StartsWith("["))
						logs.Add(line.Line);
					else
						_output.WriteLine(line.Line);
				},
				exception => _output.WriteLine($"{exception}"));
		}

		logs.Should().Contain(line => line.Contains("exclude integrations that match SqliteCommand"));
		logs.Should().Contain(line => line.Contains("exclude integrations that match AdoNet"));

		// count of manual spans without any auto instrumented spans
		apmServer.ReceivedData.Spans.Should().HaveCount(32);

		await apmServer.StopAsync();
	}

	public static IEnumerable<object[]> TargetFrameworks()
	{
		if (TestEnvironment.IsWindows)
		{
			yield return new object[] { "net5.0", "dotnet.exe" };
			yield return new object[] { "net461", "SqliteSample.exe" };
		}
		else
			yield return new object[] { "net5.0", "dotnet" };
	}

	[Theory]
	[MemberData(nameof(TargetFrameworks))]
	public async Task ShouldNotInstrumentExcludedProcess(string targetFramework, string excludeProcess)
	{
		var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
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
				null,
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

	[DisabledTestFact(
		"Sometimes fails in CI with 'Expected logs {empty} to have an item matching line.Contains(Format('service name {0} matches excluded name {1}. Profiler disabled''")]
	public async Task ShouldNotInstrumentExcludedServiceName()
	{
		var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
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
				null,
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

	[Theory]
	[InlineData("DOTNET_CLI_TELEMETRY_PROFILE", "AzureKudu")]
	[InlineData("APP_POOL_ID", "~apppool")]
	public async Task ShouldNotInstrumentAzureAppServiceInfrastructureOrReservedProcess(string key, string value)
	{
		var apmLogger = new InMemoryBlockingLogger(Logging.LogLevel.Error);
		var apmServer = new MockApmServer(apmLogger, nameof(ShouldNotInstrumentExcludedServiceName));
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

				// Azure App Service environment variables
				[AzureEnvironmentVariables.WebsiteOwnerName] = AzureEnvironmentVariables.WebsiteOwnerName,
				[AzureEnvironmentVariables.WebsiteInstanceId] = AzureEnvironmentVariables.WebsiteInstanceId,
				[AzureEnvironmentVariables.WebsiteResourceGroup] = AzureEnvironmentVariables.WebsiteResourceGroup,
				[AzureEnvironmentVariables.WebsiteSiteName] = AzureEnvironmentVariables.WebsiteSiteName,

				// Azure App Service infra/reserved process environment variable
				[key] = value
			};

			profiledApplication.Start(
				"net5.0",
				TimeSpan.FromMinutes(2),
				environmentVariables,
				null,
				line =>
				{
					if (line.Line.StartsWith("["))
						logs.Add(line.Line);
					else
						_output.WriteLine(line.Line);
				},
				exception => _output.WriteLine($"{exception}"));
		}

		logs.Should().Contain(line => line.Contains("Profiler disabled"));

		// count of manual spans without any auto instrumented spans
		apmServer.ReceivedData.Spans.Should().HaveCount(32);

		await apmServer.StopAsync();
	}
}
