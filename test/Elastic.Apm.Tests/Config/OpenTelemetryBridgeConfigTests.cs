// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Config;

/// <summary>
/// Configuration level coverage for the OpenTelemetry bridge activity source filter. The behaviour of the filter
/// itself lives in the bridge tests; these cover parsing, defaults, and the environment variable and IConfiguration
/// key wiring.
/// </summary>
[Collection("UsesEnvironmentVariables")]
public class OpenTelemetryBridgeConfigTests
{
	[Fact]
	public void EnvironmentVariableNamesAreStable()
	{
		ConfigurationOption.OpenTelemetryBridgeAllowedActivitySources.ToEnvironmentVariable()
			.Should().Be("ELASTIC_APM_OPENTELEMETRY_BRIDGE_ALLOWED_ACTIVITY_SOURCES");
		ConfigurationOption.OpenTelemetryBridgeDeniedActivitySources.ToEnvironmentVariable()
			.Should().Be("ELASTIC_APM_OPENTELEMETRY_BRIDGE_DENIED_ACTIVITY_SOURCES");
		ConfigurationOption.OpenTelemetryBridgeExperimentalSourcesEnabled.ToEnvironmentVariable()
			.Should().Be("ELASTIC_APM_OPENTELEMETRY_BRIDGE_EXPERIMENTAL_SOURCES_ENABLED");
	}

	[Fact]
	public void ConfigurationKeysAreStable()
	{
		ConfigurationOption.OpenTelemetryBridgeAllowedActivitySources.ToConfigKey()
			.Should().Be("ElasticApm:OpenTelemetryBridgeAllowedActivitySources");
		ConfigurationOption.OpenTelemetryBridgeDeniedActivitySources.ToConfigKey()
			.Should().Be("ElasticApm:OpenTelemetryBridgeDeniedActivitySources");
		ConfigurationOption.OpenTelemetryBridgeExperimentalSourcesEnabled.ToConfigKey()
			.Should().Be("ElasticApm:OpenTelemetryBridgeExperimentalSourcesEnabled");
	}

	[Fact]
	public void DefaultsAllowEverythingWithExperimentalSourcesDisabled()
	{
		var configuration = new MockConfiguration();

		WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeAllowedActivitySources, "Anything.At.All").Should().BeTrue();
		configuration.OpenTelemetryBridgeDeniedActivitySources.Should().BeEmpty();
		configuration.OpenTelemetryBridgeExperimentalSourcesEnabled.Should().BeFalse();
	}

	[Fact]
	public void ListsAreReadFromEnvironmentVariables()
	{
		var allowed = ConfigurationOption.OpenTelemetryBridgeAllowedActivitySources.ToEnvironmentVariable();
		var denied = ConfigurationOption.OpenTelemetryBridgeDeniedActivitySources.ToEnvironmentVariable();
		var experimental = ConfigurationOption.OpenTelemetryBridgeExperimentalSourcesEnabled.ToEnvironmentVariable();

		using (new ScopedEnvironmentVariable(allowed, "Microsoft.*"))
		using (new ScopedEnvironmentVariable(denied, "Microsoft.Something.Noisy"))
		using (new ScopedEnvironmentVariable(experimental, "true"))
		{
			var configuration = new EnvironmentConfiguration();

			WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeAllowedActivitySources, "Microsoft.Anything").Should().BeTrue();
			WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeAllowedActivitySources, "Other.Anything").Should().BeFalse();
			WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeDeniedActivitySources, "Microsoft.Something.Noisy").Should().BeTrue();
			configuration.OpenTelemetryBridgeExperimentalSourcesEnabled.Should().BeTrue();
		}
	}

	[Theory]
	[InlineData("A.Source,B.Source")]
	[InlineData("A.Source, B.Source")]
	[InlineData("  A.Source ,  B.Source  ")]
	public void SurroundingWhitespaceIsTrimmed(string value)
	{
		var configuration = new MockConfiguration(openTelemetryBridgeDeniedActivitySources: value);

		configuration.OpenTelemetryBridgeDeniedActivitySources.Should().HaveCount(2);
		WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeDeniedActivitySources, "A.Source").Should().BeTrue();
		WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeDeniedActivitySources, "B.Source").Should().BeTrue();
	}

	/// <summary>
	/// Matching follows the agent's usual wildcard semantics, which are case insensitive unless the pattern opts out.
	/// </summary>
	[Fact]
	public void MatchingIsCaseInsensitiveByDefault()
	{
		var configuration = new MockConfiguration(openTelemetryBridgeDeniedActivitySources: "microsoft.*");

		WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeDeniedActivitySources, "Microsoft.Extensions").Should().BeTrue();
	}

	[Fact]
	public void CaseSensitivePrefixIsHonoured()
	{
		var configuration = new MockConfiguration(openTelemetryBridgeDeniedActivitySources: "(?-i)Microsoft.*");

		WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeDeniedActivitySources, "Microsoft.Extensions").Should().BeTrue();
		WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeDeniedActivitySources, "microsoft.extensions").Should().BeFalse();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(", ,")]
	public void BlankAllowedListFallsBackToTheDefault(string value)
	{
		var configuration = new MockConfiguration(openTelemetryBridgeAllowedActivitySources: value);

		WildcardMatcher.IsAnyMatch(configuration.OpenTelemetryBridgeAllowedActivitySources, "Anything.At.All").Should().BeTrue();
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(", ,")]
	public void BlankDeniedListFallsBackToTheDefault(string value)
	{
		var configuration = new MockConfiguration(openTelemetryBridgeDeniedActivitySources: value);

		configuration.OpenTelemetryBridgeDeniedActivitySources.Should().BeEmpty();
	}

	[Theory]
	[InlineData("not-a-bool")]
	[InlineData("")]
	public void UnparseableExperimentalFlagFallsBackToTheDefault(string value)
	{
		var configuration = new MockConfiguration(openTelemetryBridgeExperimentalSourcesEnabled: value);

		configuration.OpenTelemetryBridgeExperimentalSourcesEnabled
			.Should().Be(ConfigConsts.DefaultValues.OpenTelemetryBridgeExperimentalSourcesEnabled);
	}

	[Theory]
	[InlineData("true", true)]
	[InlineData("True", true)]
	[InlineData("false", false)]
	public void ExperimentalFlagIsParsed(string value, bool expected)
	{
		var configuration = new MockConfiguration(openTelemetryBridgeExperimentalSourcesEnabled: value);

		configuration.OpenTelemetryBridgeExperimentalSourcesEnabled.Should().Be(expected);
	}

	/// <summary>
	/// Sets an environment variable for the duration of a test and restores whatever was there before, rather than
	/// clearing it, so a value present in a developer or CI environment survives the run.
	/// </summary>
	private sealed class ScopedEnvironmentVariable : IDisposable
	{
		private readonly string _name;
		private readonly string _previousValue;

		public ScopedEnvironmentVariable(string name, string value)
		{
			_name = name;
			_previousValue = Environment.GetEnvironmentVariable(name);
			Environment.SetEnvironmentVariable(name, value);
		}

		public void Dispose() => Environment.SetEnvironmentVariable(_name, _previousValue);
	}
}
