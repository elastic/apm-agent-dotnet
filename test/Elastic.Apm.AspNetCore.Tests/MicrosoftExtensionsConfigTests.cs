using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
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
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"), new TestLogger());
			Assert.Equal(LogLevel.Debug, config.LogLevel);
			Assert.Equal(new Uri("http://myServerFromTheConfigFile:8080"), config.ServerUrls[0]);
			Assert.Equal("My_Test_Application", config.ServiceName);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the LogLevel
		/// </summary>
		[Fact]
		public void ReadInvalidLogLevelConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger);
			Assert.Equal(LogLevel.Error, config.LogLevel);

			Assert.Equal(
				$"{{MicrosoftExtensionsConfig}} Failed parsing log level from {MicrosoftExtensionsConfig.Origin}: {MicrosoftExtensionsConfig.Keys.Level}, value: DbeugMisspelled. Defaulting to log level 'Error'",
				logger.Lines[0]);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the ServerUrls
		/// </summary>
		[Fact]
		public void ReadInvalidServerUrlsConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger);
			Assert.Equal(LogLevel.Error, config.LogLevel);

			Assert.Equal(
				$"{{MicrosoftExtensionsConfig}} Failed parsing log level from {MicrosoftExtensionsConfig.Origin}: {MicrosoftExtensionsConfig.Keys.Level}, value: DbeugMisspelled. Defaulting to log level 'Error'",
				logger.Lines[0]);
		}

		/// <summary>
		/// Environment variables also can be the data source fetched by IConfiguration
		/// This test makes sure that configs are applied to the agent when those are stored in env vars.
		/// </summary>
		[Fact]
		public void ReadConfingsFromEnvVarsViaIConfig()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.ConfigKeys.Level, "Debug");
			var serverUrl = "http://myServerFromEnvVar.com:1234";
			Environment.SetEnvironmentVariable(ConfigConsts.ConfigKeys.Urls, serverUrl);
			var serviceName = "MyServiceName123";
			Environment.SetEnvironmentVariable(ConfigConsts.ConfigKeys.ServiceName, serviceName);
			var secretToken = "SecretToken";
			Environment.SetEnvironmentVariable(ConfigConsts.ConfigKeys.SecretToken, secretToken);
			var configBuilder = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();

			var config = new MicrosoftExtensionsConfig(configBuilder, new TestLogger());
			Assert.Equal(LogLevel.Debug, config.LogLevel);
			Assert.Equal(new Uri(serverUrl), config.ServerUrls[0]);
			Assert.Equal(serviceName, config.ServiceName);
			Assert.Equal(secretToken, config.SecretToken);
		}

		/// <summary>
		/// Makes sure that <see cref="MicrosoftExtensionsConfig" />  logs
		/// in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			var testLogger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), testLogger);
			var serverUrl = config.ServerUrls.FirstOrDefault();
			Assert.NotNull(serverUrl);
			Assert.NotEmpty(testLogger.Lines);
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
			_capturedPayload = new MockPayloadSender();

			//The agent is instantiated with ApmMiddlewareExtension.GetService, so we can also test the calculation of the service instance.
			//(e.g. ASP.NET Core version)

			var config = new MicrosoftExtensionsConfig(
				MicrosoftExtensionsConfigTests.GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), _logger);

			_agent = new ApmAgent(
				new AgentComponents(payloadSender: _capturedPayload, configurationReader: config,
					service: ApmMiddlewareExtension.GetService(new TestAgentConfigurationReader(new TestLogger())), logger: _logger));
			_client = Helper.GetClient(_agent, _factory);
		}

		private readonly ApmAgent _agent;
		private readonly MockPayloadSender _capturedPayload;
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
			Assert.True(response.IsSuccessStatusCode);

			Assert.NotEmpty(_logger.Lines);
			Assert.Contains(_logger.Lines, n => n.Contains("Failed parsing server URL from"));
		}

		public void Dispose()
		{
			_factory?.Dispose();
			_agent?.Dispose();
			_client?.Dispose();
		}
	}
}
