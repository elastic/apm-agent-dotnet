using Elastic.Apm.Model.Payload;

namespace Elastic.Apm.Report
{
    public interface IPayloadSender
    {
        void QueuePayload(Payload payload);
    }
}
