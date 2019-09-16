using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	public class MetricsTests
	{
		private readonly ITestOutputHelper _testOutputHelper;

		public MetricsTests(ITestOutputHelper testOutputHelper) => _testOutputHelper = testOutputHelper;

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
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

			var systemTotalCpuProvider = new SystemTotalCpuProvider(new NoopLogger());
			Thread.Sleep(1000); //See https://github.com/elastic/apm-agent-dotnet/pull/264#issuecomment-499778288
			var retVal = systemTotalCpuProvider.GetSamples();
			var metricSamples = retVal as MetricSample[] ?? retVal.ToArray();

			metricSamples.First().KeyValue.Value.Should().BeGreaterOrEqualTo(0);
			metricSamples.First().KeyValue.Value.Should().BeLessOrEqualTo(1);
		}

		[Fact]
		public void ProcessCpu()
		{
			var processTotalCpuProvider = new ProcessTotalCpuTimeProvider(new NoopLogger());
			Thread.Sleep(1000); //See https://github.com/elastic/apm-agent-dotnet/pull/264#issuecomment-499778288
			var retVal = processTotalCpuProvider.GetSamples();
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

			testLogger.Lines.Count(line => line.Contains(MetricsProviderWithException.ExceptionMessage))
				.Should()
				.Be(MetricsCollector.MaxTryWithoutSuccess);

			testLogger.Lines[1].Should().Contain($"Failed reading {providerWithException.DbgName} 1 times");
			testLogger.Lines.Last(line => line.Contains("Failed reading"))
				.Should()
				.Contain(
					$"Failed reading {providerWithException.DbgName} {MetricsCollector.MaxTryWithoutSuccess} consecutively - the agent won't try reading {providerWithException.DbgName} anymore");

			//make sure GetValue() in MetricsProviderWithException is not called anymore:
			for (var i = 0; i < 10; i++) mc.CollectAllMetrics();

			var logLineBeforeStage2 = testLogger.Lines.Count;
			//no more logs, no more calls to GetValue():
			providerWithException.NumberOfGetValueCalls.Should().Be(MetricsCollector.MaxTryWithoutSuccess);
			testLogger.Lines.Count.Should().Be(logLineBeforeStage2);
		}

		[Fact]
		public async Task MetricsWithRealAgent()
		{
			// Note: If XunitOutputLogger is used with MetricsCollector it might cause issues because
			// MetricsCollector's Dispose is currently broken - it doesn't guarantee that MetricsCollector's behaves correctly (i.e., ignores)
			// timer callbacks after Dispose completed.
			// This bug in turn causes MetricsCollector to possibly use XunitOutputLogger even after the current test has exited
			// and ITestOutputHelper on which XunitOutputLogger is based became invalid.
			//
			// After https://github.com/elastic/apm-agent-dotnet/issues/494 is fixed the line below can be uncommented.
			//
			// var logger = new XunitOutputLogger(_testOutputHelper);
			//
			var logger = new NoopLogger();
			//

			var payloadSender = new MockPayloadSender();
			var configReader = new TestAgentConfigurationReader(logger, metricsInterval: "1s", logLevel: "Debug");
			using (var agent = new ApmAgent(new AgentComponents(payloadSender: payloadSender, logger: logger, configurationReader: configReader)))
			{
				await Task.Delay(10000); //make sure we wait enough to collect 1 set of metrics
				agent.ConfigurationReader.MetricsIntervalInMilliseconds.Should().Be(1000);
			}

			payloadSender.Metrics.Should().NotBeEmpty();
			payloadSender.Metrics.First().Samples.Should().NotBeEmpty();
		}

		[Theory]
		[InlineData("cpu 74608 2520 24433 1117073 6176 4054 0 0 0 0", 1117073, 1228864)]
		[InlineData("cpu  1192 0 2285 40280 626 0 376 0 0 0", 40280, 44759)]
		[InlineData("cpu    1192 0 2285 40280 626 0 376 0 0 0", 40280, 44759)]
		public void ProcStatParser(string procStatContent, long expectedIdle, long expectedTotal)
		{
			var systemTotalCpuProvider = new TestSystemTotalCpuProvider(procStatContent);
			var res = systemTotalCpuProvider.ReadProcStat();

			res.success.Should().BeTrue();
			res.idle.Should().Be(expectedIdle);
			res.total.Should().Be(expectedTotal);
		}

		private class MetricsProviderWithException : IMetricsProvider
		{
			public const string ExceptionMessage = "testException";
			public int ConsecutiveNumberOfFailedReads { get; set; }
			public string DbgName => "test metric";

			public int NumberOfGetValueCalls { get; private set; }

			public IEnumerable<MetricSample> GetSamples()
			{
				NumberOfGetValueCalls++;
				throw new Exception(ExceptionMessage);
			}
		}

		private class TestSystemTotalCpuProvider : SystemTotalCpuProvider
		{
			public TestSystemTotalCpuProvider(string procStatContent) : base(new NoopLogger(),
				new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(procStatContent)))) { }
		}
	}
}
