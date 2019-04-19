using Elastic.Apm.Api;
using Elastic.Apm.Model;

namespace Elastic.Apm.Report
{
	public interface IPayloadSender
	{
		void QueueError(IError error);

		void QueueTransaction(ITransaction transaction);

		void QueueSpan(ISpan span);
	}
}
