using System;
using System.IO;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests;
using Elastic.Apm.Tests.Mocks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.Common.ExtensionFramework;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="MicrosoftExtensionsConfig" /> class.
	/// It loads the json config files from the TestConfig folder
	/// </summary>
	public class MicrosoftExtensionsConfigTests
	{
		public MicrosoftExtensionsConfigTests() => TestHelper.ResetAgentAndEnvVars();

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_valid.json config file and passes it to the agent.
		/// Makes sure that the values from the config file are applied to the agent.
		/// </summary>
		[Fact]
		public void ReadValidConfigsFromAppSettingsJson()
		{
			var config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"));
			Assert.Equal(LogLevel.Debug, config.LogLevel);
			Assert.Equal(new Uri("http://myServerFromTheConfigFile:8080"), config.ServerUrls[0]);
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
				$"Error Config: Failed parsing log level from IConfiguration: {MicrosoftExtensionConfigConsts.LogLevel}, value: DbeugMisspelled. Defaulting to log level 'Error'",
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
				$"Error Config: Failed parsing log level from IConfiguration: {MicrosoftExtensionConfigConsts.LogLevel}, value: DbeugMisspelled. Defaulting to log level 'Error'",
				logger.Lines[0]);
		}

		/// <summary>
		/// Environment variables also can be the data source fetched by IConfiguration
		/// This test makes sure that configs are applied to the agent when those are stored in env vars.
		/// </summary>
		[Fact]
		public void ReadConfingsFromEnvVarsViaIConfig()
		{
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, "Debug");
			var serverUrl = "http://myServerFromEnvVar.com:1234";
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
			var configBuilder = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();

			var config = new MicrosoftExtensionsConfig(configBuilder);
			Assert.Equal(LogLevel.Debug, config.LogLevel);
			Assert.Equal(new Uri(serverUrl), config.ServerUrls[0]);
		}

		private IConfiguration GetConfig(string path)
			=> new ConfigurationBuilder()
				.AddJsonFile(path)
				.Build();
	}
}
