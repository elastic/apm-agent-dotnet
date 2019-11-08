using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Elastic.Apm.AspNetCore.Tests
{
	/// <summary>
	/// Tests the <see cref="ApplicationConfigurationReader" /> class.
	/// It loads the JSON config files from the TestConfig folder.
	/// </summary>
	[Collection("UsesEnvironmentVariables")]
	public class ApplicationConfigurationReaderTests
	{
		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_valid.json config file and passes it to the agent.
		/// Makes sure that the values from the config file are applied to the agent.
		/// </summary>
		[Fact]
		public void ReadValidConfigsFromAppSettingsJson()
		{
			var config = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"), new NoopLogger(), "test");
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls.Single().Should().Be(new Uri("http://myServerFromTheConfigFile:8080"));
			config.ServiceName.Should().Be("My_Test_Application");
			config.ServiceVersion.Should().Be("2.1.0.5");
			config.Environment.Should().Be("staging");
			config.CaptureHeaders.Should().BeFalse();
			config.TransactionSampleRate.Should().Be(0.456);
			config.TransactionMaxSpans.Should().Be(375);
			config.CaptureBody.Should().Be(ConfigConsts.SupportedValues.CaptureBodyAll);
			var supportedContentTypes = new[] { "application/x-www-form-urlencoded*", "text/*", "application/json*", "application/xml*" };
			config.CaptureBodyContentTypes.Should().BeEquivalentTo(supportedContentTypes);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the LogLevel
		/// </summary>
		[Fact]
		public void ReadInvalidLogLevelConfigFromAppsettingsJson()
		{
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.Environment, string.Empty);
			var logger = new TestLogger();
			var config = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger, "test");
			config.LogLevel.Should().Be(LogLevel.Error);
			logger.Lines.Should().NotBeEmpty();
			logger.Lines.Single().Should().ContainAll(
				nameof(ApplicationConfigurationReader),
				"Failed parsing log level from",
				ApplicationConfigurationReader.Origin,
				ConfigConsts.KeyNames.LogLevel,
				"Defaulting to ");

			config.Environment.Should().Be("test");
			config.CaptureHeaders.Should().BeTrue();
			config.TransactionSampleRate.Should().Be(1d);
		}

		/// <summary>
		/// Builds an IConfiguration instance with the TestConfigs/appsettings_invalid.json config file and passes it to the agent.
		/// Makes sure that the agent can deal with the invalid values contained in the config file. This tests the ServerUrls
		/// </summary>
		[Fact]
		public void ReadInvalidServerUrlsConfigFromAppsettingsJson()
		{
			var logger = new TestLogger();
			var config = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), logger, "test");
			config.LogLevel.Should().Be(LogLevel.Error);

			logger.Lines.Should().NotBeEmpty();
			logger.Lines.Single().Should()
				.ContainAll(
					nameof(ApplicationConfigurationReader),
					"Failed parsing log level from",
					ApplicationConfigurationReader.Origin,
					ConfigConsts.KeyNames.LogLevel,
					"Defaulting to ",
					"DbeugMisspelled"
				);
		}

		/// <summary>
		/// Environment variables also can be the data source fetched by IConfiguration
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
			const string serviceVersion = "2.1.0.5";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.ServiceVersion, serviceVersion);
			const string environment = "staging";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.Environment, environment);
			const string secretToken = "SecretToken";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.SecretToken, secretToken);
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.CaptureHeaders, false.ToString());
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.TransactionSampleRate, "0.123");
			var configBuilder = new ConfigurationBuilder().AddEnvironmentVariables().Build();

			var config = new ApplicationConfigurationReader(configBuilder, new NoopLogger(), "test");
			config.LogLevel.Should().Be(LogLevel.Debug);
			config.ServerUrls.Single().Should().Be(new Uri(serverUrl));
			config.ServiceName.Should().Be(serviceName);
			config.ServiceVersion.Should().Be(serviceVersion);
			config.Environment.Should().Be(environment);
			config.SecretToken.Should().Be(secretToken);
			config.CaptureHeaders.Should().BeFalse();
			config.TransactionSampleRate.Should().Be(0.123);
		}

		/// <summary>
		/// Makes sure that <see cref="ApplicationConfigurationReader" /> logs in case it reads an invalid URL.
		/// </summary>
		[Fact]
		public void LoggerNotNull()
		{
			var testLogger = new TestLogger();
			var config = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_invalid.json"), testLogger, "test");
			config.ServerUrls.FirstOrDefault().Should().NotBeNull();
			testLogger.Lines.Should().NotBeEmpty();
		}

		[Fact]
		public void MicrosoftExtensionsConfig_falls_back_on_env_vars()
		{
			var configBeforeEnvVarSet = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"), new NoopLogger(), "test");
			configBeforeEnvVarSet.FlushInterval.Should().Be(ConfigConsts.DefaultValues.FlushIntervalInMilliseconds.Milliseconds());

			var flushIntervalVal = 98.Seconds();
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.FlushInterval, (int)flushIntervalVal.TotalSeconds + "s");

			var configAfterEnvVarSet = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"), new NoopLogger(), "test");
			configAfterEnvVarSet.FlushInterval.Should().Be(flushIntervalVal);
		}

		[Fact]
		public void MicrosoftExtensionsConfig_has_precedence_over_on_env_vars()
		{
			const double transactionSampleRateEnvVarValue = 0.851;
			const double transactionSampleRateValueInAppSettings = 0.456;

			var configBeforeEnvVarSet = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"), new NoopLogger(), "test");
			configBeforeEnvVarSet.TransactionSampleRate.Should().Be(transactionSampleRateValueInAppSettings);

			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.TransactionSampleRate, transactionSampleRateEnvVarValue.ToString(CultureInfo.InvariantCulture));
			new EnvironmentConfigurationReader(new NoopLogger()).TransactionSampleRate.Should().Be(transactionSampleRateEnvVarValue);

			var configAfterEnvVarSet = new ApplicationConfigurationReader(GetConfig($"TestConfigs{Path.DirectorySeparatorChar}appsettings_valid.json"), new NoopLogger(), "test");
			configAfterEnvVarSet.TransactionSampleRate.Should().Be(transactionSampleRateValueInAppSettings);
		}

		internal static IConfiguration GetConfig(string path) => new ConfigurationBuilder().AddJsonFile(path).Build();
	}
}
