using System.Collections.Generic;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	public class MockPayloadSender : IPayloadSender
	{
		public readonly List<Error> Errors = new List<Error>();
		public readonly List<Payload> Payloads = new List<Payload>();

		public void QueueError(Error error) => Errors.Add(error);

		public void QueuePayload(Payload payload) => Payloads.Add(payload);
	}
}
