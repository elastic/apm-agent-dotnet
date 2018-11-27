using System;
using Elastic.Agent.Core.Model.Payload;
using Elastic.Agent.Core.Report;

namespace Agent.AspNetCore.Tests.Mock
{
    public class MockPayloadSender : IPayloadSender
    {
        public void QueuePayload(Payload payload)
        {
          //WIP
        }
    }
}
