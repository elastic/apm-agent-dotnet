// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Extensions.Hosting.Config;
using Elastic.Apm.Tests.Utilities;
using Elastic.Apm.Tests.Utilities.Data;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using SampleAspNetCoreApp;
using Xunit;
using static Elastic.Apm.Config.ConfigurationOption;
using Environment = System.Environment;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="ApmConfiguration" /> class.
	/// It loads the json config files from the TestConfig folder
	/// </summary>
	[Collection("UsesEnvironmentVariables")]
	public class ApmConfigurationTests
	{
		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_valid.json config file and passes it to the agent.
		/// Makes sure that the values from the config file are applied to the agent.
		/// </summary>
		[Fact]
		public void ReadValidConfigsFromAppSettingsJson()
		{
			var config = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"),
				new NoopLogger(), "test");
			config.LogLevel.Should().Be(LogLevel.Debug);
#pragma warning disable 618
			config.ServerUrls[0].Should().Be(new Uri("http://myServerFromTheConfigFile:8080"));
#pragma warning restore 618
			config.ServerUrl.Should().Be(new Uri("http://myServerFromTheConfigFile:8080"));
			config.ServiceName.Should().Be("My_Test_Application");
			config.ServiceNodeName.Should().Be("Instance1");
			config.ServiceVersion.Should().Be("2.1.0.5");
			config.Environment.Should().Be("staging");
			config.CaptureHeaders.Should().Be(false);
			config.TransactionSampleRate.Should().Be(0.456);
			config.TransactionMaxSpans.Should().Be(375);
			config.CaptureBody.Should().Be(ConfigConsts.SupportedValues.CaptureBodyAll);
			var supportedContentTypes =
				new List<string> { "application/x-www-form-urlencoded*", "text/*", "application/json*", "application/xml*" };
			config.CaptureBodyContentTypes.Should().BeEquivalentTo(supportedContentTypes);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the LogLevel
		/// </summary>
		[Fact]
		public void ReadInvalidLogLevelConfigFromAppsettingsJson()
		{
			Environment.SetEnvironmentVariable(ConfigurationOption.Environment.ToEnvironmentVariable(), "");
			var logger = new TestLogger();
			var config = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger,
				"test");
			config.LogLevel.Should().Be(LogLevel.Error);
			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					nameof(ApmConfiguration),
					"Failed parsing log level from",
					nameof(ConfigurationKeyValueProvider),
					ConfigurationOption.LogLevel.ToConfigKey(),
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
			var config = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger,
				"test");
			config.LogLevel.Should().Be(LogLevel.Error);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines[0]
				.Should()
				.ContainAll(
					nameof(ApmConfiguration),
					"Failed parsing log level from",
					nameof(ConfigurationKeyValueProvider),
					ConfigurationOption.LogLevel.ToConfigKey(),
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
			Environment.SetEnvironmentVariable(ConfigurationOption.LogLevel.ToEnvironmentVariable(), "Debug");
			var serverUrl = "http://myServerFromEnvVar.com:1234";
			Environment.SetEnvironmentVariable(ServerUrls.ToEnvironmentVariable(), serverUrl);
			var useWindowsCredentials = "true";
			Environment.SetEnvironmentVariable(UseWindowsCredentials.ToEnvironmentVariable(), useWindowsCredentials);
			var serviceName = "MyServiceName123";
			Environment.SetEnvironmentVariable(ServiceName.ToEnvironmentVariable(), serviceName);
			var serviceNodeName = "Some service node name";
			Environment.SetEnvironmentVariable(ServiceNodeName.ToEnvironmentVariable(), serviceNodeName);
			var serviceVersion = "2.1.0.5";
			Environment.SetEnvironmentVariable(ServiceVersion.ToEnvironmentVariable(), serviceVersion);
			var environment = "staging";
			Environment.SetEnvironmentVariable(ConfigurationOption.Environment.ToEnvironmentVariable(), environment);
			var secretToken = "SecretToken";
			Environment.SetEnvironmentVariable(SecretToken.ToEnvironmentVariable(), secretToken);
			var apiKey = "apiKey";
			Environment.SetEnvironmentVariable(ApiKey.ToEnvironmentVariable(), apiKey);
			Environment.SetEnvironmentVariable(CaptureHeaders.ToEnvironmentVariable(), false.ToString());
			Environment.SetEnvironmentVariable(TransactionSampleRate.ToEnvironmentVariable(), "0.123");
			var configBuilder = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();

			var config = new ApmConfiguration(configBuilder, new NoopLogger(), "test");
			config.LogLevel.Should().Be(LogLevel.Debug);
#pragma warning disable 618
			config.ServerUrls[0].Should().Be(new Uri(serverUrl));
#pragma warning restore 618
			config.ServerUrl.Should().Be(new Uri(serverUrl));
			config.UseWindowsCredentials.Should().Be(true);
			config.ServiceName.Should().Be(serviceName);
			config.ServiceNodeName.Should().Be(serviceNodeName);
			config.ServiceVersion.Should().Be(serviceVersion);
			config.Environment.Should().Be(environment);
			config.SecretToken.Should().Be(secretToken);
			config.ApiKey.Should().Be(apiKey);
			config.CaptureHeaders.Should().Be(false);
			config.TransactionSampleRate.Should().Be(0.123);
		}

		/// <summary>
		/// Makes sure that <see cref="ApmConfiguration" />  logs
		/// in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			var testLogger = new TestLogger();
			var config = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), testLogger,
				"test");
#pragma warning disable 618
			var serverUrl = config.ServerUrls.FirstOrDefault();
#pragma warning restore 618
			serverUrl.Should().NotBeNull();

			serverUrl = config.ServerUrl;
			serverUrl.Should().NotBeNull();

			testLogger.Lines.Should().NotBeEmpty();
		}

		[Fact]
		public void ApmConfiguration_falls_back_on_env_vars()
		{
			var configBeforeEnvVarSet = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"),
				new NoopLogger(), "test");
			configBeforeEnvVarSet.FlushInterval.Should().Be(ConfigConsts.DefaultValues.FlushIntervalInMilliseconds.Milliseconds());

			var flushIntervalVal = 98.Seconds();
			Environment.SetEnvironmentVariable(FlushInterval.ToEnvironmentVariable(), (int)flushIntervalVal.TotalSeconds + "s");

			var configAfterEnvVarSet = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"),
				new NoopLogger(), "test");
			configAfterEnvVarSet.FlushInterval.Should().Be(flushIntervalVal);
		}

		[Fact]
		public void ApmConfiguration_has_precedence_over_on_env_vars()
		{
			const double transactionSampleRateEnvVarValue = 0.851;
			const double transactionSampleRateValueInAppSettings = 0.456;

			var configBeforeEnvVarSet = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"),
				new NoopLogger(), "test");
			configBeforeEnvVarSet.TransactionSampleRate.Should().Be(transactionSampleRateValueInAppSettings);

			Environment.SetEnvironmentVariable(TransactionSampleRate.ToEnvironmentVariable(),
				transactionSampleRateEnvVarValue.ToString(CultureInfo.InvariantCulture));
			new EnvironmentConfiguration(new NoopLogger()).TransactionSampleRate.Should().Be(transactionSampleRateEnvVarValue);

			var configAfterEnvVarSet = new ApmConfiguration(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"),
				new NoopLogger(), "test");
			configAfterEnvVarSet.TransactionSampleRate.Should().Be(transactionSampleRateValueInAppSettings);
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
	public class ApmConfigurationIntegrationTests
		: IClassFixture<WebApplicationFactory<Startup>>, IDisposable
	{
		private readonly ApmAgent _agent;
		private readonly HttpClient _client;
		private readonly WebApplicationFactory<Startup> _factory;
		private readonly TestLogger _logger;

		public ApmConfigurationIntegrationTests(WebApplicationFactory<Startup> factory)
		{
			_factory = factory;
			_logger = new TestLogger();
			var capturedPayload = new MockPayloadSender();

			var config = new ApmConfiguration(
				ApmConfigurationTests.GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), _logger, "test");

			_agent = new ApmAgent(
				new AgentComponents(payloadSender: capturedPayload, configurationReader: config, logger: _logger));
			_client = Helper.GetClient(_agent, _factory, true);
		}

		/// <summary>
		/// Starts the app with an invalid config and
		/// makes sure the agent logs that the url was invalid.
		/// </summary>
		[Fact]
		public async Task InvalidUrlTest()
		{
			var response = await _client.GetAsync("/Home/Index");
			response.IsSuccessStatusCode.Should().BeTrue();

			_logger.Lines.Should()
				.NotBeEmpty()
				.And.Contain(n => n.Contains("Failed parsing server URL from"));
		}

		[Theory]
		[ClassData(typeof(TransactionMaxSpansTestData))]
		public void TransactionMaxSpansTest(string configurationValue, int expectedValue)
		{
			// Arrange
			var logger = new TestLogger();

			var configurationBuilder = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string> { { TransactionMaxSpans.ToConfigKey(), configurationValue } });

			var reader = new ApmConfiguration(configurationBuilder.Build(), logger, "test");

			// Act
			var transactionMaxSpans = reader.TransactionMaxSpans;

			// Assert
			transactionMaxSpans.Should().Be(expectedValue);
		}

		public void Dispose()
		{
			_factory?.Dispose();
			_agent?.Dispose();
			_client?.Dispose();
		}
	}
}
