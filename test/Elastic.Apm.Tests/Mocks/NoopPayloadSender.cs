using Elastic.Apm.Api;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	public class NoopPayloadSender : IPayloadSender
	{
		public void QueueError(IError error) { }

		public void QueueTransaction(ITransaction transaction) { }

		public void QueueSpan(ISpan span) { }

		public void QueueMetrics(IMetricSet metricSet) { }
	}
}
