// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.Config
{
	public class ProfilerLogConfigTests
	{
		[Fact]
		public void Check_Defaults()
		{
			var config = ProfilerLogConfig.Check(new NoopLogger(), new Hashtable());
			config.LogLevel.Should().Be(LogLevel.None);
			config.LogFilePath.Should().StartWith(ProfilerLogConfig.GetDefaultProfilerLogDirectory());
			config.LogFilePath.Should().EndWith(".agent.log");
			config.LogTarget.Should().Be(ProfilerLogTarget.File);
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
			var config = ProfilerLogConfig.Check(new NoopLogger(), environment);
			config.LogLevel.Should().Be(logLevel);
		}

		[Theory]
		[InlineData("foo")]
		[InlineData("tracing")]
		[InlineData(null)]
		public void Check_InvalidLogLevelValues_AreMappedToNone(string envVarValue)
		{
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG", envVarValue } };
			var config = ProfilerLogConfig.Check(new NoopLogger(), environment);
			config.LogLevel.Should().Be(LogLevel.None);
		}

		[Fact]
		public void Check_LogDir_IsEvaluatedCorrectly()
		{
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG_DIR", "/foo/bar" } };
			var config = ProfilerLogConfig.Check(new NoopLogger(), environment);
			config.LogFilePath.Should().StartWith("/foo/bar");
			config.LogFilePath.Should().EndWith(".agent.log");
		}

		[Theory]
		[InlineData(null, ProfilerLogTarget.File)]
		[InlineData("", ProfilerLogTarget.File)]
		[InlineData("foo", ProfilerLogTarget.File)]
		[InlineData("foo,bar", ProfilerLogTarget.File)]
		[InlineData("foo;bar", ProfilerLogTarget.File)]
		[InlineData("file;foo;bar", ProfilerLogTarget.File)]
		[InlineData("file", ProfilerLogTarget.File)]
		[InlineData("stdout", ProfilerLogTarget.StdOut)]
		[InlineData("StdOut", ProfilerLogTarget.StdOut)]
		[InlineData("file;stdout", ProfilerLogTarget.File | ProfilerLogTarget.StdOut)]
		[InlineData("FILE;StdOut", ProfilerLogTarget.File | ProfilerLogTarget.StdOut)]
		[InlineData("file;stdout;file", ProfilerLogTarget.File | ProfilerLogTarget.StdOut)]
		[InlineData("FILE;StdOut;stdout", ProfilerLogTarget.File | ProfilerLogTarget.StdOut)]
		internal void Check_LogTargets_AreEvaluatedCorrectly(string envVarValue, ProfilerLogTarget targets)
		{
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG_TARGETS", envVarValue } };
			var config = ProfilerLogConfig.Check(new NoopLogger(), environment);
			config.LogTarget.Should().Be(targets);
		}

		[Fact]
		public void TryApplyLogLevel_Overrides_SetLogLegel()
		{
			var logger = new TestLogger(LogLevel.Warning);
			var environment = new Hashtable { { "ELASTIC_APM_PROFILER_LOG", "trace" } };
			var config = ProfilerLogConfig.Check(new NoopLogger(), environment);
			config.TryApplyLogLevel(logger);
			logger.Level.Should().Be(LogLevel.Trace);
		}
	}
}
