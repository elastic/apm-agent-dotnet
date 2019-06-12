using Elastic.Apm.Metrics;

namespace Elastic.Apm.Tests.Mocks
{
	public class FakeMetricsCollector : IMetricsCollector
	{
		public void StartCollecting() { }
	}
}
