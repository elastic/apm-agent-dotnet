// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using FluentAssertions.Extensions;
using RichardSzalay.MockHttp;
using Xunit;
using Xunit.Abstractions;
using MockHttpMessageHandler = RichardSzalay.MockHttp.MockHttpMessageHandler;

// ReSharper disable ImplicitlyCapturedClosure

namespace Elastic.Apm.Tests.BackendCommTests.CentralConfig
{
	public class CentralConfigFetcherTests : LoggingTestBase
	{
		public CentralConfigFetcherTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

		[Fact]
		public void Should_Sanitize_HttpRequestMessage_In_Log()
		{
			var testLogger = new TestLogger(LogLevel.Trace);
			var secretToken = "secretToken";
			var serverUrl = "http://username:password@localhost:8200";

			var configSnapshotFromReader = new MockConfiguration(testLogger, logLevel: "Trace", serverUrl: serverUrl, secretToken: secretToken);
			var configStore = new ConfigurationStore(configSnapshotFromReader, testLogger);
			var service = Service.GetDefaultService(configSnapshotFromReader, testLogger);

			var waitHandle = new ManualResetEvent(false);
			var handler = new MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildGetConfigAbsoluteUrl(configSnapshotFromReader.ServerUrl, service);

			handler.When(configUrl.AbsoluteUri)
				.Respond(_ =>
				{
					waitHandle.Set();
					return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
				});

			var centralConfigFetcher = new CentralConfigurationFetcher(testLogger, configStore, service, handler);
			waitHandle.WaitOne();

			var count = 0;

			while (!testLogger.Log.Contains("Exception was thrown while fetching configuration from APM Server and parsing it.")
				&& count < 10)
			{
				Thread.Sleep(500);
				count++;
			}

			testLogger.Log
				.Should().Contain($"Authorization: {Consts.Redacted}").And.NotContain(secretToken)
				.And.NotContain(serverUrl);
		}

		[Fact]
		public void Should_Update_Logger_That_Is_ILogLevelSwitchable()
		{
			var logLevel = LogLevel.Trace;
			var testLogger = new ConsoleLogger(logLevel);

			var configSnapshotFromReader = new MockConfiguration(testLogger, logLevel: "Trace");
			var configStore = new ConfigurationStore(configSnapshotFromReader, testLogger);
			var service = Service.GetDefaultService(configSnapshotFromReader, testLogger);

			var waitHandle = new ManualResetEvent(false);
			var handler = new MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildGetConfigAbsoluteUrl(configSnapshotFromReader.ServerUrl, service);

			handler.When(configUrl.AbsoluteUri)
				.Respond(_ =>
				{
					waitHandle.Set();
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Headers = { ETag = new EntityTagHeaderValue("\"etag\"") },
						Content = new StringContent("{ \"log_level\": \"error\" }", Encoding.UTF8)
					};
				});

			var centralConfigFetcher = new CentralConfigurationFetcher(testLogger, configStore, service, handler);

			using var agent = new ApmAgent(new TestAgentComponents(testLogger,
				centralConfigurationFetcher: centralConfigFetcher,
				payloadSender: new NoopPayloadSender()));

			centralConfigFetcher.IsRunning.Should().BeTrue();
			waitHandle.WaitOne();

			// wait up to 60 seconds for the log level to change. Change can often be slower in CI
			var count = 0;
			while (count < 60 && testLogger.LogLevelSwitch.Level == logLevel)
			{
				count++;
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}

			testLogger.LogLevelSwitch.Level.Should().Be(LogLevel.Error);
		}

		/// <summary>
		/// logger that has a log level switch but does not implement <see cref="ILogLevelSwitchable"/>
		/// </summary>
		private class UnswitchableLogger: IApmLogger
		{
			public LogLevelSwitch LogLevelSwitch { get; }

			public UnswitchableLogger(LogLevelSwitch logLevelSwitch) => LogLevelSwitch = logLevelSwitch;

			public bool IsEnabled(LogLevel level) => LogLevelSwitch.Level <= level;

			public void Log<TState>(LogLevel level, TState state, Exception e, Func<TState, Exception, string> formatter)
			{
			}
		}

		[Fact]
		public void Should_Not_Update_Logger_That_Is_Not_ILogLevelSwitchable()
		{
			var testLogger = new UnswitchableLogger(new LogLevelSwitch(LogLevel.Trace));

			var configSnapshotFromReader = new MockConfiguration(testLogger, logLevel: "Trace");
			var configStore = new ConfigurationStore(configSnapshotFromReader, testLogger);
			var service = Service.GetDefaultService(configSnapshotFromReader, testLogger);

			var waitHandle = new ManualResetEvent(false);
			var handler = new MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildGetConfigAbsoluteUrl(configSnapshotFromReader.ServerUrl, service);

			handler.When(configUrl.AbsoluteUri)
				.Respond(_ =>
				{
					waitHandle.Set();
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Headers = { ETag = new EntityTagHeaderValue("\"etag\"") },
						Content = new StringContent("{ \"log_level\": \"error\" }", Encoding.UTF8)
					};
				});

			var centralConfigFetcher = new CentralConfigurationFetcher(testLogger, configStore, service, handler);
			using (var agent = new ApmAgent(new TestAgentComponents(testLogger,
				centralConfigurationFetcher: centralConfigFetcher,
				payloadSender: new NoopPayloadSender())))
			{
				centralConfigFetcher.IsRunning.Should().BeTrue();
				waitHandle.WaitOne();
				Thread.Sleep(TimeSpan.FromSeconds(3));
			}

			testLogger.LogLevelSwitch.Level.Should().Be(LogLevel.Trace);
		}

		[Fact]
		public void Should_Update_IgnoreMessageQueues_Configuration()
		{
			var configSnapshotFromReader = new MockConfiguration(LoggerBase, ignoreMessageQueues: "");
			var configStore = new ConfigurationStore(configSnapshotFromReader, LoggerBase);

			configStore.CurrentSnapshot.IgnoreMessageQueues.Should().BeEmpty();

			var service = Service.GetDefaultService(configSnapshotFromReader, LoggerBase);
			var waitHandle = new ManualResetEvent(false);
			var handler = new MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildGetConfigAbsoluteUrl(configSnapshotFromReader.ServerUrl, service);

			handler.When(configUrl.AbsoluteUri)
				.Respond(_ =>
				{
					waitHandle.Set();
					return new HttpResponseMessage(HttpStatusCode.OK)
					{
						Headers = { ETag = new EntityTagHeaderValue("\"etag\"") },
						Content = new StringContent("{ \"ignore_message_queues\": \"foo\" }", Encoding.UTF8)
					};
				});

			var centralConfigFetcher = new CentralConfigurationFetcher(LoggerBase, configStore, service, handler);

			using var agent = new ApmAgent(new TestAgentComponents(LoggerBase,
				centralConfigurationFetcher: centralConfigFetcher,
				payloadSender: new NoopPayloadSender()));

			centralConfigFetcher.IsRunning.Should().BeTrue();
			waitHandle.WaitOne();

			// wait up to 60 seconds for configuration to change. Change can often be slower in CI
			var count = 0;
			while (count < 60 && !configStore.CurrentSnapshot.IgnoreMessageQueues.Any())
			{
				count++;
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}

			configStore.CurrentSnapshot.IgnoreMessageQueues.Should().NotBeEmpty().And.Contain(m => m.GetMatcher() == "foo");
		}

		[Fact]
		public void Dispose_stops_the_thread()
		{
			CentralConfigurationFetcher lastCentralConfigurationFetcher;
			var configSnapshotFromReader = new ConfigurationSnapshotFromReader(new EnvironmentConfigurationReader(), "local");
			var configStore = new ConfigurationStore(configSnapshotFromReader, LoggerBase);
			var service = Service.GetDefaultService(new EnvironmentConfigurationReader(), LoggerBase);
			var handler = new MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildGetConfigAbsoluteUrl(configSnapshotFromReader.ServerUrl, service);
			handler.When(configUrl.AbsoluteUri)
				.Respond(_ => new HttpResponseMessage(HttpStatusCode.OK)
				{
					Headers = { ETag = new EntityTagHeaderValue("\"etag\"") },
					Content = new StringContent("{}", Encoding.UTF8)
				});

			using (var agent = new ApmAgent(new TestAgentComponents(LoggerBase,
				centralConfigurationFetcher: new CentralConfigurationFetcher(LoggerBase, configStore, service, handler),
				payloadSender: new PayloadSenderV2(LoggerBase, configSnapshotFromReader, service,
					new SystemInfoHelper(LoggerBase).GetSystemInfo(null), MockApmServerInfo.Version710))))
			{
				lastCentralConfigurationFetcher = (CentralConfigurationFetcher)agent.CentralConfigurationFetcher;
				lastCentralConfigurationFetcher.IsRunning.Should().BeTrue();

				// Sleep a few seconds to let backend component to get to the stage where they contact APM Server
				Thread.Sleep(5.Seconds());
			}
			lastCentralConfigurationFetcher.IsRunning.Should().BeFalse();
		}

		[Theory]
		[InlineData(1)]
		[InlineData(5)]
		[InlineData(9)]
		[InlineData(10)]
		[InlineData(11)]
		[InlineData(20)]
		[InlineData(40)]
		public void Create_many_concurrent_instances(int numberOfAgentInstances)
		{
			var agents = new ApmAgent[numberOfAgentInstances];

			numberOfAgentInstances.Repeat(i =>
			{
				var configSnapshotFromReader = new ConfigurationSnapshotFromReader(new EnvironmentConfigurationReader(), "local");
				var service = Service.GetDefaultService(new EnvironmentConfigurationReader(), LoggerBase);
				var configStore = new ConfigurationStore(configSnapshotFromReader, LoggerBase);

				var handler = new MockHttpMessageHandler();
				var configUrl = BackendCommUtils.ApmServerEndpoints
					.BuildGetConfigAbsoluteUrl(configSnapshotFromReader.ServerUrl, service);

				handler.When(configUrl.AbsoluteUri)
					.Respond(_ => new HttpResponseMessage(HttpStatusCode.OK)
					{
						Headers = { ETag = new EntityTagHeaderValue("\"etag\"") },
						Content = new StringContent("{}", Encoding.UTF8)
					});

				var centralConfigFetcher = new CentralConfigurationFetcher(LoggerBase, configStore, service, handler);
				var payloadSender = new PayloadSenderV2(
					LoggerBase,
					configSnapshotFromReader,
					service,
					new SystemInfoHelper(LoggerBase).GetSystemInfo(null),
					MockApmServerInfo.Version710);

				var components = new TestAgentComponents(LoggerBase, centralConfigurationFetcher: centralConfigFetcher, payloadSender: payloadSender);

				using (agents[i] = new ApmAgent(components))
				{
					payloadSender.IsRunning.Should().BeTrue();
					centralConfigFetcher.IsRunning.Should().BeTrue();
				}
			});

			// Sleep a few seconds to let backend component to get to the stage where they contact APM Server
			Thread.Sleep(5.Seconds());

			numberOfAgentInstances.Repeat(i =>
			{
				agents[i].Dispose();
				((CentralConfigurationFetcher)agents[i].CentralConfigurationFetcher).IsRunning.Should().BeFalse();
				((PayloadSenderV2)agents[i].PayloadSender).IsRunning.Should().BeFalse();
			});
		}
	}
}
