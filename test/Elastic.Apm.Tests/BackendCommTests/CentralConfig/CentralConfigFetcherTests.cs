// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Cloud;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Newtonsoft.Json;
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
		public void Should_Update_Logger_That_Is_ILogLevelSwitchable()
		{
			var testLogger = new ConsoleLogger(LogLevel.Trace);

			var environmentConfigurationReader = new EnvironmentConfigurationReader();
			var configSnapshotFromReader = new ConfigSnapshotFromReader(environmentConfigurationReader, "local");
			var configStore = new ConfigStore(configSnapshotFromReader, testLogger);
			var service = Service.GetDefaultService(environmentConfigurationReader, testLogger);

			var waitHandle = new ManualResetEvent(false);
			var handler = new MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildGetConfigAbsoluteUrl(environmentConfigurationReader.ServerUrl, service);

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

			var centralConfigFetcher = new CentralConfigFetcher(testLogger, configStore, service, handler);

			using (var agent = new ApmAgent(new TestAgentComponents(testLogger,
				centralConfigFetcher: centralConfigFetcher,
				payloadSender: new NoopPayloadSender())))
			{
				centralConfigFetcher.IsRunning.Should().BeTrue();
				waitHandle.WaitOne();
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

			var environmentConfigurationReader = new EnvironmentConfigurationReader();
			var configSnapshotFromReader = new ConfigSnapshotFromReader(environmentConfigurationReader, "local");
			var configStore = new ConfigStore(configSnapshotFromReader, testLogger);
			var service = Service.GetDefaultService(environmentConfigurationReader, testLogger);

			var waitHandle = new ManualResetEvent(false);
			var handler = new MockHttpMessageHandler();
			var configUrl = BackendCommUtils.ApmServerEndpoints
				.BuildGetConfigAbsoluteUrl(environmentConfigurationReader.ServerUrl, service);

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

			var centralConfigFetcher = new CentralConfigFetcher(testLogger, configStore, service, handler);
			using (var agent = new ApmAgent(new TestAgentComponents(testLogger,
				centralConfigFetcher: centralConfigFetcher,
				payloadSender: new NoopPayloadSender())))
			{
				centralConfigFetcher.IsRunning.Should().BeTrue();
				waitHandle.WaitOne();
				Thread.Sleep(TimeSpan.FromSeconds(1));
			}

			testLogger.LogLevelSwitch.Level.Should().Be(LogLevel.Trace);
		}

		[Fact]
		public void Dispose_stops_the_thread()
		{
			CentralConfigFetcher lastCentralConfigFetcher;
			var configSnapshotFromReader = new ConfigSnapshotFromReader(new EnvironmentConfigurationReader(), "local");
			var configStore = new ConfigStore(configSnapshotFromReader, LoggerBase);
			var service = Service.GetDefaultService(new EnvironmentConfigurationReader(), LoggerBase);
			using (var agent = new ApmAgent(new TestAgentComponents(LoggerBase,
				centralConfigFetcher: new CentralConfigFetcher(LoggerBase, configStore, service),
				payloadSender: new PayloadSenderV2(LoggerBase, configSnapshotFromReader, service,
					new SystemInfoHelper(LoggerBase).ParseSystemInfo(null), MockApmServerInfo.Version710))))
			{
				lastCentralConfigFetcher = (CentralConfigFetcher)agent.CentralConfigFetcher;
				lastCentralConfigFetcher.IsRunning.Should().BeTrue();

				// Sleep a few seconds to let backend component to get to the stage where they contact APM Server
				Thread.Sleep(5.Seconds());
			}
			lastCentralConfigFetcher.IsRunning.Should().BeFalse();
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
				var configSnapshotFromReader = new ConfigSnapshotFromReader(new EnvironmentConfigurationReader(), "local");
				var configStore = new ConfigStore(configSnapshotFromReader, LoggerBase);
				var service = Service.GetDefaultService(new EnvironmentConfigurationReader(), LoggerBase);
				using (agents[i] = new ApmAgent(new TestAgentComponents(LoggerBase,
					centralConfigFetcher: new CentralConfigFetcher(LoggerBase, configStore, service),
					payloadSender: new PayloadSenderV2(LoggerBase, configSnapshotFromReader, service,
						new SystemInfoHelper(LoggerBase).ParseSystemInfo(null), MockApmServerInfo.Version710))))
				{
					((CentralConfigFetcher)agents[i].CentralConfigFetcher).IsRunning.Should().BeTrue();
					((PayloadSenderV2)agents[i].PayloadSender).IsRunning.Should().BeTrue();
				}
			});

			// Sleep a few seconds to let backend component to get to the stage where they contact APM Server
			Thread.Sleep(5.Seconds());

			numberOfAgentInstances.Repeat(i =>
			{
				agents[i].Dispose();
				((CentralConfigFetcher)agents[i].CentralConfigFetcher).IsRunning.Should().BeFalse();
				((PayloadSenderV2)agents[i].PayloadSender).IsRunning.Should().BeFalse();
			});
		}
	}
}
