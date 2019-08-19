using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="MicrosoftExtensionsConfig" /> class. It loads the JSON config files from the TestConfig folder.
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
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls.Single().Should().Be(new Uri("http://myServerFromTheConfigFile:8080"));
			config.ServiceName.Should().Be("My_Test_Application");
			config.CaptureHeaders.Should().BeFalse();
			config.TransactionSampleRate.Should().Be(0.456);
			config.CaptureBody.Should().Be(ConfigConsts.SupportedValues.CaptureBodyAll);
			var supportedContentTypes = new List<string>() { "application/x-www-form-urlencoded*", "text/*", "application/json*", "application/xml*" };
			config.CaptureBodyContentTypes.Should().BeEquivalentTo(supportedContentTypes);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the LogLevel.
		/// </summary>
		[Fact]
		public void ReadInvalidLogLevelConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger);
			config.LogLevel.Should().Be(LogLevel.Error);
			logger.Lines.Should().NotBeEmpty();
			logger.Lines.Single().Should()
				.ContainAll(
					$"{{{nameof(MicrosoftExtensionsConfig)}}}",
					"Failed parsing log level from",
					MicrosoftExtensionsConfig.Origin,
					MicrosoftExtensionsConfig.Keys.LogLevel,
					"Defaulting to "
				);

			config.CaptureHeaders.Should().BeTrue();
			config.TransactionSampleRate.Should().Be(1.0);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the ServerUrls.
		/// </summary>
		[Fact]
		public void ReadInvalidServerUrlsConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger);
			config.LogLevel.Should().Be(LogLevel.Error);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines.Single().Should()
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
		/// Environment variables also can be the data source fetched by IConfiguration.
		/// This test makes sure that configs are applied to the agent when those are stored in env vars.
		/// </summary>
		[Fact]
		public void ReadConfigsFromEnvVarsViaIConfig()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.LogLevel, "Debug");
			const string serverUrl = "http://myServerFromEnvVar.com:1234";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServerUrls, serverUrl);
			const string serviceName = "MyServiceName123";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceName, serviceName);
			const string secretToken = "SecretToken";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.SecretToken, secretToken);
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.CaptureHeaders, false.ToString());
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.TransactionSampleRate, "0.123");
			var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables().Build();

			var config = new MicrosoftExtensionsConfig(configBuilder, new TestLogger());
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls.Single().Should().Be(new Uri(serverUrl));
			config.ServiceName.Should().Be(serviceName);
			config.SecretToken.Should().Be(secretToken);
			config.CaptureHeaders.Should().Be(false);
			config.TransactionSampleRate.Should().Be(0.123);
		}

		/// <summary>
		/// Makes sure that <see cref="MicrosoftExtensionsConfig" /> logs in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			var logger = new TestLogger();
			var serverUrl = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger).ServerUrls.FirstOrDefault();

			serverUrl.Should().NotBeNull();
			logger.Lines.Should().NotBeEmpty();
		}

		internal static IConfiguration GetConfig(string path) => new ConfigurationBuilder().AddJsonFile(path).Build();
	}
}
