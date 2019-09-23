using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	public class ConstructorTests : LoggingTestBase
	{
		public ConstructorTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		/// <summary>
		///  Assert that console logger is the default logger implementation during normal composition and that
		///  it adheres to the loglevel reported by the configuration injected into the agent
		/// </summary>
		[Fact]
		public void Compose()
		{
			using (var agent = new ApmAgent(new AgentComponents(configurationReader: new LogConfig(LogLevel.Warning)
				, dbgName: TestDisplayName)))
			{
				var logger = agent.Logger as ConsoleLogger;

				logger.Should().NotBeNull();
				logger?.IsEnabled(LogLevel.Warning).Should().BeTrue();
				logger?.IsEnabled(LogLevel.Information).Should().BeFalse();
			}
		}

		private class LogConfig : IConfigurationReader
		{
			public LogConfig(LogLevel level) => LogLevel = level;

			// ReSharper disable UnassignedGetOnlyAutoProperty
			public string CaptureBody => ConfigConsts.DefaultValues.CaptureBody;
			public List<string> CaptureBodyContentTypes { get; }
			public bool CaptureHeaders => ConfigConsts.DefaultValues.CaptureHeaders;
			public bool CentralConfig => ConfigConsts.DefaultValues.CentralConfig;
			public string Environment { get; }
			public TimeSpan FlushInterval => TimeSpan.FromMilliseconds(ConfigConsts.DefaultValues.FlushIntervalInMilliseconds);
			public LogLevel LogLevel { get; }
			public int MaxBatchEventCount => ConfigConsts.DefaultValues.MaxBatchEventCount;
			public int MaxQueueEventCount => ConfigConsts.DefaultValues.MaxQueueEventCount;
			public double MetricsIntervalInMilliseconds => ConfigConsts.DefaultValues.MetricsIntervalInMilliseconds;
			public string SecretToken { get; }
			public IReadOnlyList<Uri> ServerUrls => new List<Uri> { ConfigConsts.DefaultValues.ServerUri };
			public string ServiceName { get; }
			public string ServiceVersion { get; }
			public double SpanFramesMinDurationInMilliseconds => ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds;
			public int StackTraceLimit => ConfigConsts.DefaultValues.StackTraceLimit;

			public double TransactionSampleRate => ConfigConsts.DefaultValues.TransactionSampleRate;
			// ReSharper restore UnassignedGetOnlyAutoProperty
		}
	}
}
