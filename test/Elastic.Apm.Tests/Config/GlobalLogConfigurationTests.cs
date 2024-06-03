// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Config
{
	public class GlobalLogConfigurationTests
	{
		[Fact]
		public void Check_Defaults()
		{
			var config = GlobalLogConfiguration.FromEnvironment(new Hashtable());
			config.IsActive.Should().BeFalse();
			config.LogLevel.Should().Be(LogLevel.Warning);
			config.AgentLogFilePath.Should().StartWith(EnvironmentLoggingConfiguration.GetDefaultLogDirectory());
			config.AgentLogFilePath.Should().EndWith(".agent.log");
			config.LogTargets.Should().Be(GlobalLogTarget.File);
		}

		[Theory]
		[InlineData("trace", LogLevel.Trace)]
		[InlineData("Trace", LogLevel.Trace)]
		[InlineData("TraCe", LogLevel.Trace)]
		[InlineData("debug", LogLevel.Debug)]
		[InlineData("info", LogLevel.Information)]
		[InlineData("warn", LogLevel.Warning)]
		[InlineData("error", LogLevel.Error)]
		[InlineData("none", LogLevel.None)]
		public void Check_LogLevelValues_AreMappedCorrectly(string envVarValue, LogLevel logLevel)
		{
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG", envVarValue } };
			var config = GlobalLogConfiguration.FromEnvironment(environment);
			config.IsActive.Should().BeTrue();
			config.LogLevel.Should().Be(logLevel);
		}

		[Theory]
		[InlineData(null, false)]
		[InlineData("", false)]
		[InlineData("foo", true)]
		[InlineData("tracing", true)]
		public void Check_InvalidLogLevelValues_AreMappedToDefaultWarn(string envVarValue, bool isActive)
		{
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG", envVarValue } };
			var config = GlobalLogConfiguration.FromEnvironment(environment);
			config.LogLevel.Should().Be(LogLevel.Warning);
			config.IsActive.Should().Be(isActive);
		}

		[Fact]
		public void Check_LogDir_IsEvaluatedCorrectly()
		{
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG_DIR", "/foo/bar" } };
			var config = GlobalLogConfiguration.FromEnvironment(environment);
			config.AgentLogFilePath.Should().StartWith("/foo/bar");
			config.AgentLogFilePath.Should().EndWith(".agent.log");
		}

		[Theory]
		[InlineData(null, GlobalLogTarget.File)]
		[InlineData("", GlobalLogTarget.File)]
		[InlineData("foo", GlobalLogTarget.File)]
		[InlineData("foo,bar", GlobalLogTarget.File)]
		[InlineData("foo;bar", GlobalLogTarget.File)]
		[InlineData("file;foo;bar", GlobalLogTarget.File)]
		[InlineData("file", GlobalLogTarget.File)]
		[InlineData("stdout", GlobalLogTarget.StdOut)]
		[InlineData("StdOut", GlobalLogTarget.StdOut)]
		[InlineData("file;stdout", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		[InlineData("FILE;StdOut", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		[InlineData("file;stdout;file", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		[InlineData("FILE;StdOut;stdout", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		internal void Check_LogTargets_AreEvaluatedCorrectly(string envVarValue, GlobalLogTarget targets)
		{
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG_TARGETS", envVarValue } };
			var config = GlobalLogConfiguration.FromEnvironment(environment);
			config.LogTargets.Should().Be(targets);
		}
	}
}
