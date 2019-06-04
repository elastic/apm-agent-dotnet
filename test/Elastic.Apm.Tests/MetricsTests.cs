using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class MetricsTests
	{
		[Fact]
		public void CollectAllMetrics()
		{
			var mockPayloadSender = new MockPayloadSender();
			var testLogger = new TestLogger();
			var mc = new MetricsCollector(testLogger, mockPayloadSender, new TestAgentConfigurationReader(testLogger));

			mc.CollectAllMetrics();

			mockPayloadSender.Metrics.Should().NotBeEmpty();
		}

		[Fact]
		public void SystemCpu()
		{
			var testLogger = new TestLogger();

			var systemTotalCpuProvider = new SystemTotalCpuProvider(testLogger);

			//Needs to be called at least 2. times to deliver value - this is by design
			systemTotalCpuProvider.GetValue();
			var retVal = systemTotalCpuProvider.GetValue();
			retVal.First().KeyValue.Value.Should().BeInRange(0, 1);
		}

		[Fact]
		public void ProcessCpu()
		{
			var processTotalCpuProvider = new ProcessTotalCpuTimeProvider();

			//Needs to be called at least 2. times to deliver value - this is by design
			processTotalCpuProvider.GetValue();
			var retVal = processTotalCpuProvider.GetValue();
			retVal.First().KeyValue.Value.Should().BeInRange(0, 1);
		}

		[Fact]
		public void GetWorkingSetAndVirtualMemory()
		{
			var processWorkingSetAndVirtualMemoryProvider = new ProcessWorkingSetAndVirtualMemoryProvider();
			var retVal = processWorkingSetAndVirtualMemoryProvider.GetValue();

			var enumerable = retVal as Sample[] ?? retVal.ToArray();
			enumerable.Should().NotBeEmpty();
			enumerable.First().KeyValue.Value.Should().BeGreaterThan(0);
		}

		[Fact]
		public void ProviderWithException()
		{
			var mockPayloadSender = new MockPayloadSender();
			var testLogger = new TestLogger(LogLevel.Information);
			var mc = new MetricsCollector(testLogger, mockPayloadSender, new TestAgentConfigurationReader(testLogger, "Information"));

			mc.MetricsProviders.Clear();
			var providerWithException = new MetricsProviderWithException();
			mc.MetricsProviders.Add(providerWithException);

			for (var i = 0; i <= MetricsCollector.MaxTryWithoutSuccess; i++) mc.CollectAllMetrics();

			providerWithException.NumberOfGetValueCalls.Should().Be(MetricsCollector.MaxTryWithoutSuccess);
			testLogger.Lines.Count.Should().Be(MetricsCollector.MaxTryWithoutSuccess * 2 + 1); // *2 because exceptions are logged in a new line

			testLogger.Lines.First().Should().Contain($"Failed reading {providerWithException.NameInLogs} 1 times");
			testLogger.Lines.Last()
				.Should()
				.Contain(
					$"Failed reading {providerWithException.NameInLogs} {MetricsCollector.MaxTryWithoutSuccess} consecutively - the agent won't try reading {providerWithException.NameInLogs} anymore");

			//make sure GetValue() in MetricsProviderWithException is not called anymore:
			for (var i = 0; i < 10; i++) mc.CollectAllMetrics();

			//no more logs, no more call to GetValue():
			providerWithException.NumberOfGetValueCalls.Should().Be(MetricsCollector.MaxTryWithoutSuccess);
			testLogger.Lines.Count.Should().Be(MetricsCollector.MaxTryWithoutSuccess * 2 + 1);
		}

		[Fact]
		public async Task MetricsWithRealAgent()
		{
			var logger = new TestLogger();
			var payloadSender = new MockPayloadSender();
			var configReader = new TestAgentConfigurationReader(logger, metricsInterval: "1s");
			using (var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender, logger: logger, configurationReader: configReader)))
			{
				await Task.Delay(1200); //make sure we wait enough to collect 1 set of metrics
				agent.ConfigurationReader.MetricsIntervalInMillisecond.Should().Be(1000);
			}

			payloadSender.Metrics.Should().NotBeEmpty();
			payloadSender.Metrics.First().Samples.Should().NotBeEmpty();
		}

		private class MetricsProviderWithException : IMetricsProvider
		{
			public int ConsecutiveNumberOfFailedReads { get; set; }
			public string NameInLogs => "test metric";

			public int NumberOfGetValueCalls { get; private set; }

			public IEnumerable<Sample> GetValue()
			{
				NumberOfGetValueCalls++;
				throw new Exception("testException");
			}
		}
	}
}
