// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Features;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

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
		using var agent = new ApmAgent(new AgentComponents(null, new LogConfiguration(LogLevel.Warning), null, null,
			null, null, null));
		var logger = agent.Logger as ConsoleLogger;

		logger.Should().NotBeNull();
		logger?.IsEnabled(LogLevel.Warning).Should().BeTrue();
		logger?.IsEnabled(LogLevel.Information).Should().BeFalse();
	}

	[Fact]
	public void Ctor_MustHonor_AgentFeatures_WhenSettingUp_MetricsCollectorAndCentralConfig()
	{
		var logger = new NoopLogger();
		//
		// Default: MetricsCollector and CentralConfigurationFetcher are enabled (not null).
		//
		using (new AgentFeaturesProviderScope(new DefaultAgentFeatures(logger)))
		{
			var agentComponents = new AgentComponents();
			agentComponents.MetricsCollector.Should().NotBeNull();
			agentComponents.CentralConfigurationFetcher.Should().NotBeNull();
		}

		//
		// On Azure Functions: MetricsCollector and CentralConfigurationFetcher are disabled (null).
		//
		using (new AgentFeaturesProviderScope(new AzureFunctionsAgentFeatures(logger)))
		{
			var agentComponents = new AgentComponents();
			agentComponents.MetricsCollector.Should().BeNull();
			agentComponents.CentralConfigurationFetcher.Should().BeNull();
		}

		//
		// ... unless explicitly provided.
		//
		using (new AgentFeaturesProviderScope(new AzureFunctionsAgentFeatures(logger)))
		{
			var agentComponents = new AgentComponents(logger, null, null, new FakeMetricsCollector(), null,
				new NoopCentralConfigurationFetcher(), null);
			agentComponents.MetricsCollector.Should().NotBeNull();
			agentComponents.CentralConfigurationFetcher.Should().NotBeNull();
		}
	}

	private class LogConfiguration : IConfiguration, IConfigurationSnapshotDescription
	{
		public LogConfiguration(LogLevel level) => LogLevel = level;

		public string Description => "LogConfig";

		// ReSharper disable UnassignedGetOnlyAutoProperty
		public string CaptureBody => ConfigConsts.DefaultValues.CaptureBody;
		public bool Recording { get; }
		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => ConfigConsts.DefaultValues.SanitizeFieldNames;
		public List<string> CaptureBodyContentTypes { get; }
		public bool CaptureHeaders => ConfigConsts.DefaultValues.CaptureHeaders;
		public bool CentralConfig => ConfigConsts.DefaultValues.CentralConfig;
		public string CloudProvider => ConfigConsts.DefaultValues.CloudProvider;
		public bool Enabled { get; }
		public string Environment { get; }
		public string ServiceNodeName { get; }
		public double ExitSpanMinDuration => ConfigConsts.DefaultValues.ExitSpanMinDurationInMilliseconds;

		public TimeSpan FlushInterval =>
			TimeSpan.FromMilliseconds(ConfigConsts.DefaultValues.FlushIntervalInMilliseconds);

		public IReadOnlyDictionary<string, string> GlobalLabels => new Dictionary<string, string>();
		public string HostName { get; }
		public string TraceContinuationStrategy => ConfigConsts.DefaultValues.TraceContinuationStrategy;
		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls => ConfigConsts.DefaultValues.TransactionIgnoreUrls;
		public LogLevel LogLevel { get; }
		public int MaxBatchEventCount => ConfigConsts.DefaultValues.MaxBatchEventCount;
		public int MaxQueueEventCount => ConfigConsts.DefaultValues.MaxQueueEventCount;
		public double MetricsIntervalInMilliseconds => ConfigConsts.DefaultValues.MetricsIntervalInMilliseconds;
		public string SecretToken { get; }
		public string ServerCert { get; }
		public string ApiKey { get; }
		public IReadOnlyList<Uri> ServerUrls => new List<Uri> { ConfigConsts.DefaultValues.ServerUri };
		public bool UseWindowsCredentials { get; }
		public Uri ServerUrl => ConfigConsts.DefaultValues.ServerUri;
		public string ServiceName { get; }
		public string ServiceVersion { get; }
		public bool SpanCompressionEnabled => ConfigConsts.DefaultValues.SpanCompressionEnabled;

		public double SpanCompressionExactMatchMaxDuration =>
			ConfigConsts.DefaultValues.SpanCompressionExactMatchMaxDurationInMilliseconds;

		public double SpanCompressionSameKindMaxDuration =>
			ConfigConsts.DefaultValues.SpanCompressionSameKindMaxDurationInMilliseconds;

		public IReadOnlyList<WildcardMatcher> DisableMetrics => ConfigConsts.DefaultValues.DisableMetrics;
		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues => ConfigConsts.DefaultValues.IgnoreMessageQueues;

		public double SpanStackTraceMinDurationInMilliseconds =>
			ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds;

		[Obsolete("Use SpanStackTraceMinDurationInMilliseconds")]
		public double SpanFramesMinDurationInMilliseconds =>
			ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds;

		public int StackTraceLimit => ConfigConsts.DefaultValues.StackTraceLimit;
		public double TransactionSampleRate => ConfigConsts.DefaultValues.TransactionSampleRate;

		public bool VerifyServerCert => ConfigConsts.DefaultValues.VerifyServerCert;

		public bool EnableOpenTelemetryBridge => ConfigConsts.DefaultValues.EnableOpenTelemetryBridge;

		public bool TraceContextIgnoreSampledFalse => ConfigConsts.DefaultValues.TraceContextIgnoreSampledFalse;

		public IReadOnlyCollection<string> ExcludedNamespaces => ConfigConsts.DefaultValues.DefaultExcludedNamespaces;

		public IReadOnlyCollection<string> ApplicationNamespaces =>
			ConfigConsts.DefaultValues.DefaultApplicationNamespaces;

		public bool UseElasticTraceparentHeader => ConfigConsts.DefaultValues.UseElasticTraceparentHeader;

		public int TransactionMaxSpans => ConfigConsts.DefaultValues.TransactionMaxSpans;
		// ReSharper restore UnassignedGetOnlyAutoProperty
	}
}
