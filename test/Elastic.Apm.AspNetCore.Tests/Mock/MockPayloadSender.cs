using System;
using System.Collections.Generic;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.AspNetCore.Tests.Mock
{
    public class MockPayloadSender : IPayloadSender
    {
        public List<Payload> Payloads = new List<Payload>();
        public void QueuePayload(Payload payload) => Payloads.Add(payload);
    }
}
