using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class ConstructorTests
	{
		private class LogConfig : IConfigurationReader
		{
			public LogConfig(LogLevel level) => LogLevel = level;
			public LogLevel LogLevel { get; }
			public IReadOnlyList<Uri> ServerUrls => new List<Uri> { ConfigConsts.DefaultValues.ServerUri };
			public string ServiceName { get; }
			public string SecretToken { get; }
			public bool CaptureHeaders { get; }
			public double TransactionSampleRate { get; }
			public string CaptureBody { get; }
			public double MetricsIntervalInMilliseconds { get; }
			public int StackTraceLimit { get; }
			public double SpanFramesMinDurationInMilliseconds { get; }
			public List<string> CaptureBodyContentTypes { get; }
		}

		///<summary>
		/// Assert that console logger is the default logger implementation during normal composition and that
		/// it adheres to the loglevel reported by the configuration injected into the agent
		///</summary>
		[Fact]
		public void Compose()
		{
			var agent = new ApmAgent(new AgentComponents(configurationReader: new LogConfig(LogLevel.Warning)));
			var logger = agent.Logger as ConsoleLogger;

			logger.Should().NotBeNull();
			logger?.IsEnabled(LogLevel.Warning).Should().BeTrue();
			logger?.IsEnabled(LogLevel.Information).Should().BeFalse();
		}
	}
}
