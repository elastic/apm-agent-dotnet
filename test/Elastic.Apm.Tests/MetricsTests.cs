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
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.Tests
{
	public class MetricsTests : LoggingTestBase
	{
		private const string ThisClassName = nameof(MetricsTests);

		private readonly IApmLogger _logger;

		public MetricsTests(ITestOutputHelper xUnitOutputHelper) : base(xUnitOutputHelper) => _logger = LoggerBase.Scoped(ThisClassName);

		public static IEnumerable<object[]> DisableProviderTestData
		{
			get
			{
				yield return new object[] { null };
				yield return new object[] { new List<MetricSample>() };
				yield return new object[] { new List<MetricSample> { new MetricSample("key", double.NaN) } };
				yield return new object[] { new List<MetricSample> { new MetricSample("key", double.NegativeInfinity) } };
				yield return new object[] { new List<MetricSample> { new MetricSample("key", double.PositiveInfinity) } };
			}
		}

		[Fact]
		public void CollectAllMetrics()
		{
			var mockPayloadSender = new MockPayloadSender();
			using (var mc = new MetricsCollector(_logger, mockPayloadSender, new MockConfigSnapshot(_logger)))
				mc.CollectAllMetrics();

			mockPayloadSender.Metrics.Should().NotBeEmpty();
		}

		[Fact]
		public void SystemCpu()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

			using var systemTotalCpuProvider = new SystemTotalCpuProvider(new NoopLogger());
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
			var processWorkingSetAndVirtualMemoryProvider = new ProcessWorkingSetAndVirtualMemoryProvider(true, true);
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
			using (var mc = new MetricsCollector(testLogger, mockPayloadSender, new MockConfigSnapshot(testLogger, "Information")))
			{
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
			// var logger = _logger;
			//
			var logger = new NoopLogger();
			//

			var payloadSender = new MockPayloadSender();
			var configReader = new MockConfigSnapshot(logger, metricsInterval: "1s", logLevel: "Debug");
			using var agentComponents = new AgentComponents(payloadSender: payloadSender, logger: logger, configurationReader: configReader);
			using (var agent = new ApmAgent(agentComponents))
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
			using var systemTotalCpuProvider = new TestSystemTotalCpuProvider(procStatContent);
			var res = systemTotalCpuProvider.ReadProcStat();

			res.success.Should().BeTrue();
			res.idle.Should().Be(expectedIdle);
			res.total.Should().Be(expectedTotal);
		}

		[Theory]
		[MemberData(nameof(DisableProviderTestData))]
		public void CollectAllMetrics_ShouldDisableProvider_WhenSamplesAreInvalid(List<MetricSample> samples)
		{
			const int iterations = MetricsCollector.MaxTryWithoutSuccess * 2;

			// Arrange
			var logger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();

			using var metricsCollector = new MetricsCollector(logger, mockPayloadSender, new MockConfigSnapshot(logger, "Information"));
			var metricsProviderMock = new Mock<IMetricsProvider>();

			metricsProviderMock.Setup(x => x.IsMetricAlreadyCaptured).Returns(true);

			metricsProviderMock
				.Setup(x => x.GetSamples())
				.Returns(() => samples);
			metricsProviderMock.SetupProperty(x => x.ConsecutiveNumberOfFailedReads);

			metricsCollector.MetricsProviders.Clear();
			metricsCollector.MetricsProviders.Add(metricsProviderMock.Object);

			// Act
			foreach (var _ in Enumerable.Range(0, iterations)) metricsCollector.CollectAllMetrics();

			// Assert
			mockPayloadSender.Metrics.Should().BeEmpty();
			metricsProviderMock.Verify(x => x.GetSamples(), Times.Exactly(MetricsCollector.MaxTryWithoutSuccess));
		}

		[Fact]
		public void CollectAllMetrics_ShouldNotDisableProvider_WhenAnyValueIsSamplesIsValid()
		{
			const int iterations = MetricsCollector.MaxTryWithoutSuccess * 2;

			// Arrange
			var logger = new NoopLogger();
			var mockPayloadSender = new MockPayloadSender();

			using var metricsCollector = new MetricsCollector(logger, mockPayloadSender, new MockConfigSnapshot(logger, "Information"));

			var metricsProviderMock = new Mock<IMetricsProvider>();

			metricsProviderMock.Setup(x => x.IsMetricAlreadyCaptured).Returns(true);

			metricsProviderMock.Setup(x => x.GetSamples())
				.Returns(() => new List<MetricSample> { new MetricSample("key1", double.NaN), new MetricSample("key2", 0.95) });
			metricsProviderMock.SetupProperty(x => x.ConsecutiveNumberOfFailedReads);

			metricsCollector.MetricsProviders.Clear();
			metricsCollector.MetricsProviders.Add(metricsProviderMock.Object);

			// Act
			foreach (var _ in Enumerable.Range(0, iterations)) metricsCollector.CollectAllMetrics();

			// Assert
			mockPayloadSender.Metrics.Count.Should().Be(iterations);
			mockPayloadSender.Metrics.Should().OnlyContain(x => x.Samples.Count() == 1);
			metricsProviderMock.Verify(x => x.GetSamples(), Times.Exactly(iterations));
		}

		[Fact]
		public void CollectGcMetrics()
		{
			using (var gcMetricsProvider = new GcMetricsProvider(_logger))
			{
				gcMetricsProvider.IsMetricAlreadyCaptured.Should().BeFalse();

				// ReSharper disable once TooWideLocalVariableScope
				// ReSharper disable once RedundantAssignment
				var containsValue = false;

#if !NETCOREAPP2_1
				//EventSource Microsoft-Windows-DotNETRuntime is only 2.2+, no gc metrics on 2.1
				//repeat the allocation multiple times and make sure at least 1 GetSamples() call returns value
				for (var j = 0; j < 100; j++)
				{
					Thread.Sleep(500);
					for (var i = 0; i < 300_000; i++)
					{
						var _ = new int[100];
					}

					Thread.Sleep(1000);

					for (var i = 0; i < 300_000; i++)
					{
						var _ = new int[100];
					}

					GC.Collect();

					Thread.Sleep(1000);

					var samples = gcMetricsProvider.GetSamples();

					containsValue = samples != null && samples.Count() != 0;

					if (containsValue)
						break;
				}

				containsValue.Should().BeTrue();
				gcMetricsProvider.IsMetricAlreadyCaptured.Should().BeTrue();
#endif
			}
		}

		internal class MetricsProviderWithException : IMetricsProvider
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

			public bool IsMetricAlreadyCaptured => true;
		}

		internal class TestSystemTotalCpuProvider : SystemTotalCpuProvider
		{
			public TestSystemTotalCpuProvider(string procStatContent) : base(new NoopLogger(),
				new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(procStatContent)))) { }
		}
	}
}
