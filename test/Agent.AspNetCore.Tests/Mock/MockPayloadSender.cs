using System;
using System.Collections.Generic;
using Elastic.Agent.Core.Model.Payload;
using Elastic.Agent.Core.Report;

namespace Agent.AspNetCore.Tests.Mock
{
    public class MockPayloadSender : IPayloadSender
    {
        public List<Payload> Payloads = new List<Payload>();
        public void QueuePayload(Payload payload) => Payloads.Add(payload);
    }
}
