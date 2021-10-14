// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Cloud;
using Elastic.Apm.Logging;
using Elastic.Apm.Profiler.Managed.Tests.AdoNet;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Profiler.Managed.Tests
{
	public class ExcludeTests
	{
		private readonly ITestOutputHelper _output;

		public ExcludeTests(ITestOutputHelper output) => _output = output;

		[Fact]
		public async Task ShouldNotInstrumentExcludedIntegrations()
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
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
					["ELASTIC_APM_PROFILER_LOG_TARGETS"] = "file;stdout",
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

			logs.Should().Contain(line => line.Contains("exclude integrations that match SqliteCommand"));
			logs.Should().Contain(line => line.Contains("exclude integrations that match AdoNet"));

			// count of manual spans without any auto instrumented spans
			apmServer.ReceivedData.Spans.Should().HaveCount(32);

			await apmServer.StopAsync();
		}

		[Theory]
		[InlineData("net5.0", "dotnet.exe")]
		[InlineData("net461", "SqliteSample.exe")]
		public async Task ShouldNotInstrumentExcludedProcess(string targetFramework, string excludeProcess)
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
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

		[Fact]
		public async Task ShouldNotInstrumentExcludedServiceName()
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
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

		[Theory]
		[InlineData("DOTNET_CLI_TELEMETRY_PROFILE", "AzureKudu")]
		[InlineData("APP_POOL_ID", "~apppool")]
		public async Task ShouldNotInstrumentAzureAppServiceInfrastructureOrReservedProcess(string key, string value)
		{
			var apmLogger = new InMemoryBlockingLogger(Elastic.Apm.Logging.LogLevel.Error);
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
					[AzureAppServiceMetadataProvider.WebsiteOwnerName] = AzureAppServiceMetadataProvider.WebsiteOwnerName,
					[AzureAppServiceMetadataProvider.WebsiteInstanceId] = AzureAppServiceMetadataProvider.WebsiteInstanceId,
					[AzureAppServiceMetadataProvider.WebsiteResourceGroup] = AzureAppServiceMetadataProvider.WebsiteResourceGroup,
					[AzureAppServiceMetadataProvider.WebsiteSiteName] = AzureAppServiceMetadataProvider.WebsiteSiteName,

					// Azure App Service infra/reserved process environment variable
					[key] = value
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

			logs.Should().Contain(line => line.Contains($"Profiler disabled"));

			// count of manual spans without any auto instrumented spans
			apmServer.ReceivedData.Spans.Should().HaveCount(32);

			await apmServer.StopAsync();
		}
	}
}
