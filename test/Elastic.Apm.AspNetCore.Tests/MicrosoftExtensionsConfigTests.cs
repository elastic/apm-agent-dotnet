using System;
using System.IO;
using Elastic.Apm.AspNetCore.Config;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests;
using Elastic.Apm.Tests.Mocks;
using Microsoft.Extensions.Configuration;
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
		public void ReadValidConfigsFromAppsettingsJson()
		{
			Agent.Config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"));
			Assert.Equal(LogLevel.Debug, Agent.Config.LogLevel);
			Assert.Equal(new Uri("http://myServerFromTheConfigFile:8080"), Agent.Config.ServerUrls[0]);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the LogLevel
		/// </summary>
		[Fact]
		public void ReadInvalidLogLevelConfigFromAppsettingsJson()
		{
			Agent.SetLoggerType<TestLogger>();

			Agent.Config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"));
			Assert.Equal(LogLevel.Error, Agent.Config.LogLevel);

			Assert.Equal(
				$"Error Config: Failed parsing log level from IConfiguration: {MicrosoftExtensionConfigConsts.LogLevel}, value: DbeugMisspelled. Defaulting to log level 'Error'",
				(Agent.Config.Logger as TestLogger).Lines[0]);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the ServerUrls
		/// </summary>
		[Fact]
		public void ReadInvalidServerUrlsConfigFromAppsettingsJson()
		{
			Agent.SetLoggerType<TestLogger>();

			Agent.Config = new MicrosoftExtensionsConfig(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"));
			Assert.Equal(LogLevel.Error, Agent.Config.LogLevel);

			Assert.Equal(
				$"Error Config: Failed parsing log level from IConfiguration: {MicrosoftExtensionConfigConsts.LogLevel}, value: DbeugMisspelled. Defaulting to log level 'Error'",
				(Agent.Config.Logger as TestLogger).Lines[0]);
		}

		/// <summary>
		/// Environment variables also can be the data source fetched by IConfiguration
		/// This test makes sure that configs are applied to the agent when those are stored in envvars.
		/// </summary>
		[Fact]
		public void ReadConfingsFromEnvVarsViaIConfig()
		{
			Environment.SetEnvironmentVariable(EnvVarConsts.LogLevel, "Debug");
			var serverUrl = "http://myServerFromEnvVar.com:1234";
			Environment.SetEnvironmentVariable(EnvVarConsts.ServerUrls, serverUrl);
			var config = new ConfigurationBuilder()
				.AddEnvironmentVariables()
				.Build();

			Agent.Config = new MicrosoftExtensionsConfig(config);
			Assert.Equal(LogLevel.Debug, Agent.Config.LogLevel);
			Assert.Equal(new Uri(serverUrl), Agent.Config.ServerUrls[0]);
		}

		private IConfiguration GetConfig(string path)
			=> new ConfigurationBuilder()
				.AddJsonFile(path)
				.Build();
	}
}
