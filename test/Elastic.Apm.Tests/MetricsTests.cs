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
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	public class MetricsTests
	{
		private readonly ITestOutputHelper _outputHelper;

		public MetricsTests(ITestOutputHelper outputHelper)
		{
			_outputHelper = outputHelper;
		}

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
			var testLogger = new XUnitLogger(_outputHelper);
			var systemTotalCpuProvider = new SystemTotalCpuProvider(testLogger);

			//Needs to be called at least 2 times to deliver value - this is by design
			systemTotalCpuProvider.GetSamples();
			var retVal = systemTotalCpuProvider.GetSamples();


			retVal.First().KeyValue.Value.Should().BeInRange(0, 1);
		}

		[Fact]
		public void ProcessCpu()
		{
			var testLogger = new XUnitLogger(_outputHelper);
			var processTotalCpuProvider = new ProcessTotalCpuTimeProvider();
			var retVal = processTotalCpuProvider.GetSamples2(testLogger);
			retVal.First().KeyValue.Value.Should().BeInRange(0, 1);
		}

		[Fact]
		public void GetWorkingSetAndVirtualMemory()
		{
			var processWorkingSetAndVirtualMemoryProvider = new ProcessWorkingSetAndVirtualMemoryProvider();
			var retVal = processWorkingSetAndVirtualMemoryProvider.GetSamples();

			var enumerable = retVal as MetricSample[] ?? retVal.ToArray();
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

			for (var i = 0; i < MetricsCollector.MaxTryWithoutSuccess; i++) mc.CollectAllMetrics();

			providerWithException.NumberOfGetValueCalls.Should().Be(MetricsCollector.MaxTryWithoutSuccess);

			// *2 because exceptions are logged in a new line, + 2 because 1) printing the metricsinterval and 2) printing that the given metrics
			// wont be collected anymore:
			testLogger.Lines.Count.Should().Be(MetricsCollector.MaxTryWithoutSuccess * 2 + 2);

			testLogger.Lines[1].Should().Contain($"Failed reading {providerWithException.DbgName} 1 times");
			testLogger.Lines.Last()
				.Should()
				.Contain(
					$"Failed reading {providerWithException.DbgName} {MetricsCollector.MaxTryWithoutSuccess} consecutively - the agent won't try reading {providerWithException.DbgName} anymore");

			//make sure GetValue() in MetricsProviderWithException is not called anymore:
			for (var i = 0; i < 10; i++) mc.CollectAllMetrics();

			//no more logs, no more call to GetValue():
			providerWithException.NumberOfGetValueCalls.Should().Be(MetricsCollector.MaxTryWithoutSuccess);
			testLogger.Lines.Count.Should().Be(MetricsCollector.MaxTryWithoutSuccess * 2 + 2);
		}

		[Fact]
		public async Task MetricsWithRealAgent()
		{
			var logger = new TestLogger();
			var payloadSender = new MockPayloadSender();
			var configReader = new TestAgentConfigurationReader(logger, metricsInterval: "1s");
			using (var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender, logger: logger, configurationReader: configReader)))
			{
				await Task.Delay(2200); //make sure we wait enough to collect 1 set of metrics
				agent.ConfigurationReader.MetricsIntervalInMillisecond.Should().Be(1000);
			}

			payloadSender.Metrics.Should().NotBeEmpty();
			payloadSender.Metrics.First().Samples.Should().NotBeEmpty();
		}

		private class MetricsProviderWithException : IMetricsProvider
		{
			public int ConsecutiveNumberOfFailedReads { get; set; }
			public string DbgName => "test metric";

			public int NumberOfGetValueCalls { get; private set; }

			public IEnumerable<MetricSample> GetSamples()
			{
				NumberOfGetValueCalls++;
				throw new Exception("testException");
			}
		}
	}
}
