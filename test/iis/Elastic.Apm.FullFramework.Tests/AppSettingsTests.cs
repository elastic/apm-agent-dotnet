// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information
#if NETFRAMEWORK
using System.Configuration;
using Elastic.Apm.Config;
using Elastic.Apm.Config.Net4FullFramework;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Config.ConfigurationOption;
using static Elastic.Apm.Config.Net4FullFramework.FullFrameworkDefaultImplementations;
using Environment = System.Environment;
using LogLevel = Elastic.Apm.Logging.LogLevel;

namespace Elastic.Apm.FullFramework.Tests
{
	// Referenced later by string
	// ReSharper disable once UnusedType.Global
	internal class ConfigTestReader : FallbackToEnvironmentConfigurationBase
	{
		private static readonly ConfigurationDefaults ConfigurationDefaults = new() { DebugName = nameof(ConfigTestReader) };

		public ConfigTestReader(IApmLogger? logger = null)
			: base(logger, ConfigurationDefaults, new AppSettingsConfigurationKeyValueProvider(logger)) { }
	}

	[Collection("UsesEnvironmentVariables")]
	public class AppSettingsTests
	{
		private static void UpdateAppSettings(Dictionary<ConfigurationOption, string> values)
		{
			var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var appSettings = (AppSettingsSection)config.GetSection("appSettings");
			appSettings.Settings.Clear();
			foreach (var v in values) appSettings.Settings.Add(v.Key.ToConfigKey(), v.Value);
			config.Save();
			ConfigurationManager.RefreshSection("appSettings");
		}

		[Fact]
		public void CustomEnvironmentTest()
		{
			UpdateAppSettings(new Dictionary<ConfigurationOption, string>());
			Environment.SetEnvironmentVariable(ConfigurationOption.Environment.ToEnvironmentVariable(), "Development");
			var config = new AppSettingsConfiguration();
			config.Environment.Should().Be("Development");

			UpdateAppSettings(new Dictionary<ConfigurationOption, string> { { ConfigurationOption.Environment, "Staging" } });
			config = new AppSettingsConfiguration();

			// app-settings should have priority over environment variables
			config.Environment.Should().Be("Staging");
		}

		[Fact]
		public void CustomFlushIntervalTest()
		{
			UpdateAppSettings(new Dictionary<ConfigurationOption, string>());

			Environment.SetEnvironmentVariable(FlushInterval.ToEnvironmentVariable(), "10ms");
			var config = new AppSettingsConfiguration();
			config.FlushInterval.Should().Be(TimeSpan.FromMilliseconds(10));

			UpdateAppSettings(new Dictionary<ConfigurationOption, string> { { FlushInterval, "20ms" } });
			config = new AppSettingsConfiguration();

			// app-settings should have priority over environment variables
			config.FlushInterval.Should().Be(TimeSpan.FromMilliseconds(20));
		}

		[Fact]
		public void CreateConfigurationReaderThroughApSettings()
		{
			var logger = new ConsoleLogger(LogLevel.Information);
			var config = new Dictionary<ConfigurationOption, string>();

			var type = "Elastic.Apm.Tests.Config.ConfigTestReader, Elastic.Apm.Tests";
			config.Add(FullFrameworkConfigurationReaderType, type);

			UpdateAppSettings(config);

			var reader = CreateConfigurationReaderFromConfiguredType(logger);

			Assert.NotNull(reader);
		}

		[Fact]
		public void CreateConfigurationReaderThroughEnvVar()
		{
			var logger = new ConsoleLogger(LogLevel.Information);
			var config = new Dictionary<ConfigurationOption, string>();

			var type = "Elastic.Apm.Tests.Config.ConfigTestReader, Elastic.Apm.Tests";
			Environment.SetEnvironmentVariable(FullFrameworkConfigurationReaderType.ToEnvironmentVariable(), type);

			UpdateAppSettings(config);
			var reader = CreateConfigurationReaderFromConfiguredType(logger);

			Assert.NotNull(reader);
		}
	}
}
#endif
