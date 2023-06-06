// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Config;

public class ConfigurationLoggingPreambleTests
{
	[Fact]
	public void OptionLoggingInstructions_MustContainAll_IConfigurationReader_Properties()
	{
		foreach (var propertyName in typeof(IConfigurationReader).GetProperties().Select(p => p.Name))
		{
			// Extra treatment required?
			var n = propertyName;
			if (n.EndsWith("InMilliseconds"))
				n = propertyName.Replace("InMilliseconds", string.Empty);
			ConfigurationLogger.OptionLoggingInstructions.Should().Contain(i => i.Option.ToString() == n);
		}
	}

	[Fact]
	public void OptionLoggingInstructions_ThatAlwaysLog_MustProvideDefaultValue()
	{
		var configuration = new MockConfiguration(serviceVersion: "42");
		var logAlwaysInstructions = ConfigurationLogger.OptionLoggingInstructions.Where(i => i.LogAlways).ToArray();
		logAlwaysInstructions.Should().NotBeEmpty();
		foreach (var item in logAlwaysInstructions)
		{
			var kv = ConfigurationLogger.GetDefaultValueForLogging(item.Option, configuration);
			kv.Value.Should().NotBeNullOrEmpty($"for {item}");
			kv.Value.Should().NotBe("UnknownDefaultValue");
		}
	}

	[Fact]
	public void PrintAgentLogPreamble_Prints_ActiveConfigAndDefaults()
	{
		var configuration = new MockConfiguration(serviceVersion: "42");
		var logger = new TestLogger(LogLevel.Information);
		ConfigurationLogger.PrintAgentLogPreamble(logger, configuration);
		logger.Lines.Should().NotContain(s => s.Contains("[Warning]"));
		logger.Lines.Should().OnlyContain(s => s.Contains("[Info]"), logger.Log);

		//We expect the IConfigKeyValueProvider and IEnvironmentKeyValueProvider to be logged explicitly
		var description = $"via '{nameof(MockConfiguration)} (config provider: {nameof(NullConfigurationKeyValueProvider)}"
			+ $" environment provider: {nameof(MockConfigurationEnvironmentProvider)})'";

		logger.Lines.Should().Contain(s => s.Contains(description), logger.Log);
		logger.Lines.Should().Contain(s => s.Contains("Environment->service_version: '42'"), logger.Log);
		logger.Lines.Should().Contain(s => s.Contains("Default->server_url: 'http"), logger.Log);
		logger.Lines.Should().Contain(s => s.Contains("Default->log_level: 'Error'"), logger.Log);
	}

	[Fact]
	public void PrintAgentLogPreamble_RedactsSensitiveInformation()
	{
		var configuration = new MockConfiguration(
			secretToken: "42",
			serverUrl: $"http://abc:def@localhost:8123",
			serverUrls: $"http://ghi:jkl@localhost:8124,http://mno:pqr@localhost:8125"
		);
		var logger = new TestLogger(LogLevel.Information);
		ConfigurationLogger.PrintAgentLogPreamble(logger, configuration);
		logger.Lines.Should().NotContain(s => s.Contains("[Warning]"));
		logger.Lines.Should().OnlyContain(s => s.Contains("[Info]"), logger.Log);
		logger.Lines.Should().NotContain(s => s.Contains("Environment->secret_token: '42'"), logger.Log);
		logger.Lines.Should().NotContain(s => s.Contains("Default->secret_token: '42'"), logger.Log);
		logger.Lines.Should().Contain(s => s.Contains("Environment->secret_token: '[REDACTED]'"), logger.Log);

		logger.Lines.Should().NotContain(s => s.Contains("server_url: 'http://abc:def"), logger.Log);
		logger.Lines.Should().Contain(s => s.Contains("server_url: 'http://[REDACTED]:[REDACTED]"), logger.Log);

		logger.Lines.Should().NotContain(s => s.Contains("server_url: 'http://abc:def"), logger.Log);
		logger.Lines.Should().Contain(s => s.Contains("server_url: 'http://[REDACTED]:[REDACTED]"), logger.Log);
		var redactedServerUrls = "server_urls: 'http://[REDACTED]:[REDACTED]@localhost:8124/,http://[REDACTED]:[REDACTED]";
		logger.Lines.Should().Contain(s => s.Contains(redactedServerUrls), logger.Log);
	}

	[Fact]
	public void ConfigurationStoreLogsUpdates()
	{
		var configuration = new RuntimeConfigurationSnapshot(new EnvironmentConfiguration());
		var logger = new TestLogger(LogLevel.Information);
		var configStore = new ConfigurationStore(configuration, logger);

		//since the configuration has no update from central config yet
		logger.Lines.Should().Contain(s => s.Contains(" Agent Configuration (via 'EnvironmentConfiguration"), logger.Log);

		var centralConfigPayload = new CentralConfigurationResponseParser.CentralConfigPayload(new Dictionary<string, string>
		{
			{ DynamicConfigurationOption.StackTraceLimit.ToJsonKey(), "1200" }
		});
		var centralConfig = new CentralConfiguration(logger, centralConfigPayload, "1337");
		configStore.CurrentSnapshot = new RuntimeConfigurationSnapshot(new EnvironmentConfiguration(), centralConfig);

		// Now that the current snapshot holds a central configuration we expect ConfigurationStore
		// To log the active configuration again this time indicating config comes from CentralConfig and EnvironmentConfiguration
		var configDescription = "Agent Configuration (via 'Central Config (Etag: 1337) + EnvironmentConfiguration";
		logger.Lines.Should().Contain(s => s.Contains(configDescription), logger.Log);

		// logs indicate the current config has a new active configuration from the central config
		logger.Lines.Should().Contain(s => s.Contains("CentralConfig->stack_trace_limit: '1200'"), logger.Log);
	}
}
