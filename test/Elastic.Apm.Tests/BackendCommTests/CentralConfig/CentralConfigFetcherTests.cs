// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.BackendComm.CentralConfig;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;

// ReSharper disable ImplicitlyCapturedClosure

namespace Elastic.Apm.Tests.BackendCommTests.CentralConfig
{
	public class CentralConfigFetcherTests : LoggingTestBase
	{
		public CentralConfigFetcherTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) { }

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
					new SystemInfoHelper(LoggerBase).ParseSystemInfo(null), new MockServerInfo()))))
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
						new SystemInfoHelper(LoggerBase).ParseSystemInfo(null), new MockServerInfo()))))
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
