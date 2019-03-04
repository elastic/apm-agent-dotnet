using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Report
{
	public interface IPayloadSender
	{
		void QueueError(IError error);

		void QueuePayload(IPayload payload);

		void QueueTransaction(ITransaction transaction);

		void QueueSpan(ISpan span);
	}
}
