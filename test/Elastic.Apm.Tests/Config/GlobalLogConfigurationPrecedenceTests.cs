// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Logging.LogEnvironmentVariables;

namespace Elastic.Apm.Tests.Config;

public class GlobalLogConfigurationPrecedenceTests
{
	[Fact]
	public void CheckLogLevelPrecedence()
	{
		var config = CreateConfig([
			(OTEL_LOG_LEVEL, "trace"),
			(ELASTIC_APM_PROFILER_LOG, "info"),
			(ELASTIC_APM_LOG_LEVEL, "error"),
		]);
		config.LogLevel.Should().Be(LogLevel.Trace);

		config = CreateConfig([
			(ELASTIC_APM_PROFILER_LOG, "info"),
			(ELASTIC_APM_LOG_LEVEL, "error"),
		]);
		config.LogLevel.Should().Be(LogLevel.Information);

		config = CreateConfig([
			(ELASTIC_APM_LOG_LEVEL, "error"),
		]);
		config.LogLevel.Should().Be(LogLevel.Error);
	}

	[Fact]
	public void CheckLogDirPrecedence()
	{
		var config = CreateConfig([
			(OTEL_DOTNET_AUTO_LOG_DIRECTORY, nameof(OTEL_DOTNET_AUTO_LOG_DIRECTORY)),
			(ELASTIC_APM_PROFILER_LOG_DIR, nameof(ELASTIC_APM_PROFILER_LOG_DIR)),
			(ELASTIC_APM_LOG_DIRECTORY, nameof(ELASTIC_APM_LOG_DIRECTORY)),
		]);
		config.LogFileDirectory.Should().Be(nameof(OTEL_DOTNET_AUTO_LOG_DIRECTORY));

		config = CreateConfig([
			(ELASTIC_APM_PROFILER_LOG_DIR, nameof(ELASTIC_APM_PROFILER_LOG_DIR)),
			(ELASTIC_APM_LOG_DIRECTORY, nameof(ELASTIC_APM_LOG_DIRECTORY)),
		]);
		config.LogFileDirectory.Should().Be(nameof(ELASTIC_APM_PROFILER_LOG_DIR));

		config = CreateConfig([
			(ELASTIC_APM_LOG_DIRECTORY, nameof(ELASTIC_APM_LOG_DIRECTORY)),
		]);
		config.LogFileDirectory.Should().Be(nameof(ELASTIC_APM_LOG_DIRECTORY));
	}

	[Fact]
	public void CheckLogTargetsPrecedence()
	{
		var config = CreateConfig([
			(ELASTIC_OTEL_LOG_TARGETS, "stdout"),
			(ELASTIC_APM_PROFILER_LOG_TARGETS, "stdout;file"),
		]);
		config.LogTargets.Should().Be(GlobalLogTarget.StdOut);

		config = CreateConfig([
			(ELASTIC_APM_PROFILER_LOG_TARGETS, "stdout;file"),
		]);
		config.LogTargets.Should().Be(GlobalLogTarget.StdOut | GlobalLogTarget.File);
	}

	private static GlobalLogConfiguration CreateConfig(params (string key, string v)[] values)
	{
		var environment = new Hashtable();
		foreach (var (key, v) in values)
			environment.Add(key, v);
		var config = GlobalLogConfiguration.FromEnvironment(environment);
		return config;
	}
}
