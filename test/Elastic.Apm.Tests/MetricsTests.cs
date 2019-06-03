using System.Linq;
using Elastic.Apm.Api;
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
	}
}
