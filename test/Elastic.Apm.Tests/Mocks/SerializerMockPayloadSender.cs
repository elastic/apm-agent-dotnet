using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Tests.Mocks
{
	/// <summary>
	/// A mock IPayloadSender that serializes the events and the deserializes them again.
	/// This is useful when you'd like to test agent features that rely on serialization.
	/// </summary>
	internal class SerializerMockPayloadSender : IPayloadSender
	{
		private readonly PayloadItemSerializer _payloadItemSerializer;

		public SerializerMockPayloadSender(IConfigurationReader configurationReader) => _payloadItemSerializer = new PayloadItemSerializer(configurationReader);

		public Transaction FirstTransaction => Transactions.First() as Transaction;
		public Error FirstError => Errors.First() as Error;

		public List<IError> Errors { get; } = new List<IError>();
		public List<Transaction> Transactions { get; } = new List<Transaction>();

		public void QueueError(IError error)
		{
			var item = _payloadItemSerializer.SerializeObject(error);
			var deserializedError = JsonConvert.DeserializeObject<Error>(item);
			Errors.Add(deserializedError);
		}

		public void QueueMetrics(IMetricSet metrics) { }

		public void QueueSpan(ISpan span) { }

		public void QueueTransaction(ITransaction transaction)
		{
			var item = _payloadItemSerializer.SerializeObject(transaction);
			var deserializedTransaction = JsonConvert.DeserializeObject<Transaction>(item);
			Transactions.Add(deserializedTransaction);
		}
	}
}
