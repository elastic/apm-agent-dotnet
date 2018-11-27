using System;
using Elastic.Agent.Core.Model.Payload;

namespace Elastic.Agent.Core.Report
{
    public interface IPayloadSender
    {
        void QueuePayload(Payload payload);
    }
}
