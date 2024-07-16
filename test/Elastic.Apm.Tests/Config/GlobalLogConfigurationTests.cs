// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Logging.LogEnvironmentVariables;

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
			//because is active is false log targets defaults to none;
			config.LogTargets.Should().Be(GlobalLogTarget.None);
		}


		//
		[Theory]
		[InlineData(OTEL_LOG_LEVEL, "Info")]
		[InlineData(ELASTIC_APM_PROFILER_LOG, "Info")]

		[InlineData(OTEL_DOTNET_AUTO_LOG_DIRECTORY, "1")]
		[InlineData(ELASTIC_APM_LOG_DIRECTORY, "1")]
		[InlineData(ELASTIC_APM_PROFILER_LOG_DIR, "1")]
		[InlineData(ELASTIC_APM_STARTUP_HOOKS_LOGGING, "1")]
		//only if explicitly specified to 'none' should we not default to file logging.
		[InlineData(ELASTIC_OTEL_LOG_TARGETS, "file")]
		[InlineData(ELASTIC_APM_PROFILER_LOG_TARGETS, "file")]
		public void CheckActivation(string environmentVariable, string value)
		{
			var config = GlobalLogConfiguration.FromEnvironment(new Hashtable { { environmentVariable, value } });
			config.IsActive.Should().BeTrue();
			config.LogTargets.Should().Be(GlobalLogTarget.File);
		}

		//
		[Theory]
		[InlineData(OTEL_LOG_LEVEL, "none")]
		[InlineData(ELASTIC_APM_PROFILER_LOG, "None")]
		//only if explicitly specified to 'none' should we not default to file logging.
		[InlineData(ELASTIC_OTEL_LOG_TARGETS, "none")]
		[InlineData(ELASTIC_APM_PROFILER_LOG_TARGETS, "none")]
		public void CheckDeactivation(string environmentVariable, string value)
		{
			var config = GlobalLogConfiguration.FromEnvironment(new Hashtable
			{
				{ OTEL_DOTNET_AUTO_LOG_DIRECTORY, "" },
				{ environmentVariable, value }
			});
			config.IsActive.Should().BeFalse();
			config.LogTargets.Should().Be(GlobalLogTarget.None);
		}

		[Theory]
		//only specifying apm_log_level not sufficient, needs explicit directory configuration
		[InlineData(ELASTIC_APM_LOG_LEVEL, "Warning")]
		//setting targets to none will result in no global trace logging
		[InlineData(ELASTIC_OTEL_LOG_TARGETS, "None")]
		[InlineData(ELASTIC_APM_PROFILER_LOG_TARGETS, "None")]
		//setting file log level to none will result in no global trace logging
		[InlineData(OTEL_LOG_LEVEL, "None")]
		//setting profiler log level to none will result in no global trace logging
		[InlineData(ELASTIC_APM_PROFILER_LOG, "None")]
		public void CheckNonActivation(string environmentVariable, string value)
		{
			var config = GlobalLogConfiguration.FromEnvironment(new Hashtable { { environmentVariable, value } });
			config.IsActive.Should().BeFalse();
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
			Check(ELASTIC_APM_PROFILER_LOG, envVarValue, logLevel);
			Check(ELASTIC_APM_LOG_LEVEL, envVarValue, logLevel);
			Check(OTEL_LOG_LEVEL, envVarValue, logLevel);
			return;

			static void Check(string key, string envVarValue, LogLevel level)
			{
				var config = CreateConfig(key, envVarValue);
				config.LogLevel.Should().Be(level, "{0}", key);
			}
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("foo")]
		[InlineData("tracing")]
		public void Check_InvalidLogLevelValues_AreMappedToDefaultWarn(string envVarValue)
		{
			Check(ELASTIC_APM_PROFILER_LOG, envVarValue);
			Check(ELASTIC_APM_LOG_LEVEL, envVarValue);
			Check(OTEL_LOG_LEVEL, envVarValue);
			return;

			static void Check(string key, string envVarValue)
			{
				var config = CreateConfig(key, envVarValue);
				config.LogLevel.Should().Be(LogLevel.Warning, "{0}", key);
			}
		}

		[Fact]
		public void Check_LogDir_IsEvaluatedCorrectly()
		{
			Check(ELASTIC_APM_PROFILER_LOG_DIR, "/foo/bar");
			Check(ELASTIC_APM_LOG_DIRECTORY, "/foo/bar");
			Check(OTEL_DOTNET_AUTO_LOG_DIRECTORY, "/foo/bar");
			return;

			static void Check(string key, string envVarValue)
			{
				var config = CreateConfig(key, envVarValue);
				config.AgentLogFilePath.Should().StartWith("/foo/bar", "{0}", key);
				config.AgentLogFilePath.Should().EndWith(".agent.log", "{0}", key);
			}
		}

		[Theory]
		[InlineData(null, GlobalLogTarget.None)]
		[InlineData("", GlobalLogTarget.None)]
		[InlineData("foo", GlobalLogTarget.None)]
		[InlineData("foo,bar", GlobalLogTarget.None)]
		[InlineData("foo;bar", GlobalLogTarget.None)]
		[InlineData("file;foo;bar", GlobalLogTarget.File)]
		[InlineData("file", GlobalLogTarget.File)]
		[InlineData("stdout", GlobalLogTarget.StdOut)]
		[InlineData("StdOut", GlobalLogTarget.StdOut)]
		[InlineData("file;stdout", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		[InlineData("FILE;StdOut", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		[InlineData("file;stdout;file", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		[InlineData("FILE;StdOut;stdout", GlobalLogTarget.File | GlobalLogTarget.StdOut)]
		internal void Check_LogTargets_AreEvaluatedCorrectly(string envVarValue, GlobalLogTarget? targets)
		{
			Check(ELASTIC_APM_PROFILER_LOG_TARGETS, envVarValue, targets);
			Check(ELASTIC_OTEL_LOG_TARGETS, envVarValue, targets);
			return;

			static void Check(string key, string envVarValue, GlobalLogTarget? targets)
			{
				var config = CreateConfig(key, envVarValue);
				config.LogTargets.Should().Be(targets, "{0}", key);
			}
		}

		private static GlobalLogConfiguration CreateConfig(string key, string envVarValue)
		{
			var environment = new Hashtable { { key, envVarValue } };
			var config = GlobalLogConfiguration.FromEnvironment(environment);
			return config;
		}
	}
}
