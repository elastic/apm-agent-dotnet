using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Model.Payload;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	internal class MockPayloadSender : IPayloadSender
	{
		public readonly List<IError> Errors = new List<IError>();
		public readonly List<IPayload> Payloads = new List<IPayload>();

		public Error.ErrorDetail FirstErrorDetail => Errors[0].Errors[0] as Error.ErrorDetail;

		/// <summary>
		/// The 1. Span on the 1. Transaction
		/// </summary>
		public Span FirstSpan => (Payloads[0].Transactions[0] as Transaction)?.Spans[0] as Span;

		public Transaction FirstTransaction => Payloads[0].Transactions[0] as Transaction;

		public Span[] SpansOnFirstTransaction => (Payloads[0].Transactions[0] as Transaction)?.Spans as Span[];

		public void QueueError(IError error) => Errors.Add(error);

		public void QueuePayload(IPayload payload) => Payloads.Add(payload);

		public void QueueTransaction(ITransaction transaction) => throw new System.NotImplementedException();
	}
}
