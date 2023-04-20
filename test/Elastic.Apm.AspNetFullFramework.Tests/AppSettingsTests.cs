// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
using System;
using System.Collections.Generic;
using System.Configuration;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	internal class ConfigTestReader : FallbackToEnvironmentConfigurationBase
	{
		public ConfigTestReader(IApmLogger logger = null)
			: base(logger, null, nameof(ConfigTestReader), new AppSettingsConfigurationKeyValueProvider(logger))
		{ }
	}

	[Collection("UsesEnvironmentVariables")]
	public class AppSettingsTests
	{
		private static void UpdateAppSettings(Dictionary<string, string> values)
		{
			var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var appSettings = (AppSettingsSection)config.GetSection("appSettings");
			appSettings.Settings.Clear();
			foreach (var v in values) appSettings.Settings.Add(v.Key, v.Value);
			config.Save();
			ConfigurationManager.RefreshSection("appSettings");
		}

		[Fact]
		public void CustomEnvironmentTest()
		{
			UpdateAppSettings(new Dictionary<string, string>());

			var config = new ElasticApmModuleConfiguration();

			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.Environment, "Development");

			config.Environment.Should().Be("Development");

			UpdateAppSettings(new Dictionary<string, string> { { ConfigConsts.KeyNames.Environment, "Staging" } });

			config.Environment.Should().Be("Staging");
		}

		[Fact]
		public void CustomFlushIntervalTest()
		{
			UpdateAppSettings(new Dictionary<string, string>());

			var config = new ElasticApmModuleConfiguration();

			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.FlushInterval, "10ms");

			config.FlushInterval.Should().Be(TimeSpan.FromMilliseconds(10));

			UpdateAppSettings(new Dictionary<string, string> { { ConfigConsts.KeyNames.FlushInterval, "20ms" } });

			config.FlushInterval.Should().Be(TimeSpan.FromMilliseconds(20));
		}

		[Fact]
		public void CreateConfigurationReaderThroughApSettings()
		{
			var logger = new ConsoleLogger(LogLevel.Information);
			var config = new Dictionary<string, string>();

			var type = "Elastic.Apm.AspNetFullFramework.Tests.ConfigTestReader, Elastic.Apm.AspNetFullFramework.Tests";

			config.Add(ConfigConsts.KeyNames.FullFrameworkConfigurationReaderType, type);

			UpdateAppSettings(config);

			var reader = Helper.ConfigHelper.CreateReader(logger);

			Assert.NotNull(reader);
		}

		[Fact]
		public void CreateConfigurationReaderThroughEnvVar()
		{
			var logger = new ConsoleLogger(LogLevel.Information);
			var config = new Dictionary<string, string>();

			var type = "Elastic.Apm.AspNetFullFramework.Tests.ConfigTestReader, Elastic.Apm.AspNetFullFramework.Tests";
			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.FullFrameworkConfigurationReaderType, type);

			UpdateAppSettings(config);
			var reader = Helper.ConfigHelper.CreateReader(logger);

			Assert.NotNull(reader);
		}
	}
}
