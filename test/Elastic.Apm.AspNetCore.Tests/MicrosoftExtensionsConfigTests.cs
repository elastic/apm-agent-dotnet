using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SampleAspNetCoreApp;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="MicrosoftExtensionsConfig" /> class.
	/// It loads the json config files from the TestConfig folder
	/// </summary>
	public class MicrosoftExtensionsConfigTests
	{
		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_valid.json config file and passes it to the agent.
		/// Makes sure that the values from the config file are applied to the agent.
		/// </summary>
		[Fact]
		public void ReadValidConfigsFromAppSettingsJson()
		{
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"),
				new NoopLogger(), "test");
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls[0].Should().Be(new Uri("http://myServerFromTheConfigFile:8080"));
			config.ServiceName.Should().Be("My_Test_Application");
			config.ServiceVersion.Should().Be("2.1.0.5");
			config.Environment.Should().Be("staging");
			config.CaptureHeaders.Should().Be(false);
			config.TransactionSampleRate.Should().Be(0.456);
			config.CaptureBody.Should().Be(ConfigConsts.SupportedValues.CaptureBodyAll);
			var supportedContentTypes = new List<string>() { "application/x-www-form-urlencoded*", "text/*", "application/json*", "application/xml*" };
			config.CaptureBodyContentTypes.Should().BeEquivalentTo(supportedContentTypes);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the LogLevel
		/// </summary>
		[Fact]
		public void ReadInvalidLogLevelConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger,
				"test");
			config.LogLevel.Should().Be(LogLevel.Error);
			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0].Should()
				.ContainAll(
					$"{{{nameof(MicrosoftExtensionsConfig)}}}",
					"Failed parsing log level from",
					MicrosoftExtensionsConfig.Origin,
					MicrosoftExtensionsConfig.Keys.LogLevel,
					"Defaulting to "
				);

			config.Environment.Should().Be("test");
			config.CaptureHeaders.Should().Be(true);
			config.TransactionSampleRate.Should().Be(1.0);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the ServerUrls
		/// </summary>
		[Fact]
		public void ReadInvalidServerUrlsConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger,
				"test");
			config.LogLevel.Should().Be(LogLevel.Error);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0].Should()
				.ContainAll(
					$"{{{nameof(MicrosoftExtensionsConfig)}}}",
					"Failed parsing log level from",
					MicrosoftExtensionsConfig.Origin,
					MicrosoftExtensionsConfig.Keys.LogLevel,
					"Defaulting to ",
					"DbeugMisspelled"
				);
		}

		/// <summary>
		/// Environment variables also can be the data source fetched by IConfiguration
		/// This test makes sure that configs are applied to the agent when those are stored in env vars.
		/// </summary>
		[Fact]
		public void ReadConfingsFromEnvVarsViaIConfig()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.LogLevel, "Debug");
			var serverUrl = "http://myServerFromEnvVar.com:1234";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServerUrls, serverUrl);
			var serviceName = "MyServiceName123";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceName, serviceName);
			var serviceVersion = "2.1.0.5";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceVersion, serviceVersion);
			var environment = "staging";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.Environment, environment);
			var secretToken = "SecretToken";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.SecretToken, secretToken);
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.CaptureHeaders, false.ToString());
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.TransactionSampleRate, "0.123");
			var configBuilder = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();

			var config = new MicrosoftExtensionsConfig(configBuilder, new NoopLogger(), "test");
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls[0].Should().Be(new Uri(serverUrl));
			config.ServiceName.Should().Be(serviceName);
			config.ServiceVersion.Should().Be(serviceVersion);
			config.Environment.Should().Be(environment);
			config.SecretToken.Should().Be(secretToken);
			config.CaptureHeaders.Should().Be(false);
			config.TransactionSampleRate.Should().Be(0.123);
		}

		/// <summary>
		/// Makes sure that <see cref="MicrosoftExtensionsConfig" />  logs
		/// in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			var testLogger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), testLogger,
				"test");
			var serverUrl = config.ServerUrls.FirstOrDefault();
			serverUrl.Should().NotBeNull();
			testLogger.Lines.Should().NotBeEmpty();
		}

		internal static IConfiguration GetConfig(string path)
			=> new ConfigurationBuilder()
				.AddJsonFile(path)
				.Build();
	}

	/// <summary>
	/// Tests that use a real ASP.NET Core application.
	/// </summary>
	[Collection("DiagnosticListenerTest")] //To avoid tests from DiagnosticListenerTests running in parallel with this we add them to 1 collection.
	public class MicrosoftExtensionsConfigIntegrationTests
		: IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		public MicrosoftExtensionsConfigIntegrationTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_logger = new TestLogger();
			var capturedPayload = new MockPayloadSender();

			var config = new MicrosoftExtensionsConfig(
				MicrosoftExtensionsConfigTests.GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), _logger, "test");

			_agent = new ApmAgent(
				new AgentComponents(payloadSender: capturedPayload, configurationReader: config, logger: _logger));
			_client = Helper.GetClient(_agent, _factory);
		}

		private readonly ApmAgent _agent;
		private readonly HttpClient _client;
		private readonly WebApplicationFactory<Startup> _factory;
		private readonly TestLogger _logger;

		/// <summary>
		/// Starts the app with an invalid config and
		/// makes sure the agent logs that the url was invalid.
		/// </summary>
		[Fact]
		public async Task InvalidUrlTest()
		{
			var response = await _client.GetAsync("/Home/Index");
			response.IsSuccessStatusCode.Should().BeTrue();

			_logger.Lines.Should().NotBeEmpty()
				.And.Contain(n => n.Contains("Failed parsing server URL from"));
		}

		public void Dispose()
		{
			_factory?.Dispose();
			_agent?.Dispose();
			_client?.Dispose();
		}
	}
}
