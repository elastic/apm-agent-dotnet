using System.Threading.Tasks;
using Elastic.Apm.Metrics;
using Elastic.Apm.Tests.Mocks;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class MetricsTests
	{
		[Fact]
		public async Task TestCollectAllMetrics()
		{
			var mockPayloadSender = new MockPayloadSender();
			var testLogger = new TestLogger();
			var mc = new MetricsCollector(testLogger, mockPayloadSender);

			mc.CollectAllMetrics();

			mockPayloadSender.Metrics.Should().NotBeEmpty();
		}
	}
}
