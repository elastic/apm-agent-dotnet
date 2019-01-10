using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Report
{
	public interface IPayloadSender
	{
		void QueueError(Error error);

		void QueuePayload(Payload payload);
	}
}
