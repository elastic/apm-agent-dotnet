// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Linq;
using System.Text.RegularExpressions;
using Elastic.Apm.Config;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Config;

public class ConfigurationLoggingPreambleTests
{
	[Fact]
	public void ConfigurationItemId_MapsCorrectly_ToEnvironmentVariable_And_ConfigKey()
	{
		static string CamelCaseRegex(string input)
		{
			return string.Join("", Regex.Matches(input, "(^[A-Z]+(?![a-z])|[A-Z][a-z]+)")
				.OfType<Match>()
				.Select(m => m.Value.ToLower()));
		}

		foreach (var item in ConfigurationLoggingPreamble.ConfigurationItems)
		{
			// Lowercase and concat all tokens of Id, EnvironmentVariableName and ConfigKeyName. They must match then.

			// Id
			var flatId = CamelCaseRegex(item.Option.ToString());

			// EnvironmentVariableName
			var flatEnvVar = string.Join("",
				item.EnvironmentVariableName.Substring(ConfigurationOptionExtensions.EnvPrefix.Length).Split('_')
					.Select(t => t.ToLower()));
			flatId.Should().Be(flatEnvVar);

			// ConfigurationKeyName
			var flatConfigKey =
				CamelCaseRegex(item.ConfigurationKeyName.Substring(ConfigurationOptionExtensions.KeyPrefix.Length));
			flatEnvVar.Should().Be(flatConfigKey);
		}
	}

	[Fact]
	public void ConfigurationItems_MustContainAll_IConfigurationReader_Properties()
	{
		foreach (var propertyName in typeof(IConfigurationReader).GetProperties().Select(p => p.Name))
		{
			// Extra treatment required?
			var n = propertyName;
			if (n.EndsWith("InMilliseconds")) n = propertyName.Replace("InMilliseconds", string.Empty);
			ConfigurationLoggingPreamble.ConfigurationItems.Should().Contain(i => i.Option.ToString() == n);
		}
	}

	[Fact]
	public void ConfigurationItems_ThatAlwaysLog_MustProvideDefaultValue()
	{
		var configuration = new MockConfiguration(serviceVersion: "42");
		foreach (var item in ConfigurationLoggingPreamble.ConfigurationItems.Where(i => i.LogAlways))
		{
			var kv = ConfigurationLoggingPreamble.GetDefaultValueForLogging(item.Option, configuration, "unknown");
			kv.Value.Should().NotBeNullOrEmpty($"for {item}");
			kv.Value.Should().NotBe("UnknownDefaultValue");
		}
	}
}
