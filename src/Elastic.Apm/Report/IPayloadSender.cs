using Elastic.Apm.Api;

namespace Elastic.Apm.Report
{
	public interface IPayloadSender
	{
		void QueueError(IError error);

		void QueueMetrics(IMetricSet metrics);

		void QueueSpan(ISpan span);

		void QueueTransaction(ITransaction transaction);
	}
}
