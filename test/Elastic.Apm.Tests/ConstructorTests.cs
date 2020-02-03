using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class ConstructorTests
	{
		/// <summary>
		///  Assert that console logger is the default logger implementation during normal composition and that
		///  it adheres to the loglevel reported by the configuration injected into the agent
		/// </summary>
		[Fact]
		public void Compose()
		{
			//build AgentComponents manually so we can disable metrics collection. reason: creating metrics collector pro test and disposing it makes test failing (ETW or EventSource subscribe unsubscribe in each test in parallel if all tests are running)
			var agent = new ApmAgent(new AgentComponents(null, configurationReader: new LogConfig(LogLevel.Warning), null, metricsCollector: null,
				null, null));
			var logger = agent.Logger as ConsoleLogger;

			logger.Should().NotBeNull();
			logger?.IsEnabled(LogLevel.Warning).Should().BeTrue();
			logger?.IsEnabled(LogLevel.Information).Should().BeFalse();
		}

		private class LogConfig : IConfigSnapshot
		{
			public LogConfig(LogLevel level) => LogLevel = level;

			// ReSharper disable UnassignedGetOnlyAutoProperty
			public string CaptureBody => ConfigConsts.DefaultValues.CaptureBody;
			public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => ConfigConsts.DefaultValues.SanitizeFieldNames;
			public List<string> CaptureBodyContentTypes { get; }
			public bool CaptureHeaders => ConfigConsts.DefaultValues.CaptureHeaders;
			public bool CentralConfig => ConfigConsts.DefaultValues.CentralConfig;
			public string Environment { get; }
			public string ServiceNodeName { get; }
			public TimeSpan FlushInterval => TimeSpan.FromMilliseconds(ConfigConsts.DefaultValues.FlushIntervalInMilliseconds);
			public IReadOnlyDictionary<string, string> GlobalLabels => new Dictionary<string, string>();
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

			public int TransactionMaxSpans => ConfigConsts.DefaultValues.TransactionMaxSpans;
			// ReSharper restore UnassignedGetOnlyAutoProperty

			public string DbgDescription => "LogConfig";
		}
	}
}
