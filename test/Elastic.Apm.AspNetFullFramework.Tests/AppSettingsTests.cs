using System;
using System.Collections.Generic;
using System.Configuration;
using Elastic.Apm.Config;
using Xunit;

namespace Elastic.Apm.AspNetFullFramework.Tests.FullFramework
{
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

			var config = new FullFrameworkConfigReader();

			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.Environment, "Development");

			config.Environment.Should().Be("Development");

			UpdateAppSettings(new Dictionary<string, string> { { ConfigConsts.KeyNames.Environment, "Staging" } });

			config.Environment.Should().Be("Staging");
		}

		[Fact]
		public void CustomFlushIntervalTest()
		{
			UpdateAppSettings(new Dictionary<string, string>());

			var config = new FullFrameworkConfigReader();

			Environment.SetEnvironmentVariable(ConfigConsts.EnvVarNames.FlushInterval, "10ms");

			config.FlushInterval.Should().Be(TimeSpan.FromMilliseconds(10));

			UpdateAppSettings(new Dictionary<string, string> { { ConfigConsts.KeyNames.FlushInterval, "20ms" } });

			config.FlushInterval.Should().Be(TimeSpan.FromMilliseconds(20));
		}
	}
}
