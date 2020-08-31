// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
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

		private readonly TaskCompletionSource<ITransaction> _transactionTaskCompletionSource = new TaskCompletionSource<ITransaction>();
		private readonly TaskCompletionSource<IError> _errorTaskCompletionSource = new TaskCompletionSource<IError>();
		private readonly List<Transaction> _transactions = new List<Transaction>();
		private readonly List<Error> _errors = new List<Error>();

		public SerializerMockPayloadSender(IConfigurationReader configurationReader) => _payloadItemSerializer = new PayloadItemSerializer(configurationReader);

		public Transaction FirstTransaction
		{
			get
			{
				_transactionTaskCompletionSource.Task.Wait();
				return Transactions.First();
			}
		}

		public Error FirstError => Errors.First() as Error;

		public List<Error> Errors
		{

			get
			{

				var timer = new Timer
				{
					Interval = 1000
				};

				timer.Enabled = true;
				timer.Start();

				timer.Elapsed += (a, b) =>
				{
					_errorTaskCompletionSource.TrySetCanceled();
					timer.Stop();
				};


				try
				{
					_errorTaskCompletionSource.Task.Wait();
				}
				catch
				{
					return null;
				}

				return _errors;
			}
		}

		public List<Transaction> Transactions
		{
			get
			{

				var timer = new Timer
				{
					Interval = 1000
				};

				timer.Enabled = true;
				timer.Start();

				timer.Elapsed += (a, b) =>
				{
					_transactionTaskCompletionSource.TrySetCanceled();
					timer.Stop();
				};

				_transactionTaskCompletionSource.Task.Wait();
				return _transactions;
			}
		}

		public void QueueError(IError error)
		{
			var item = _payloadItemSerializer.SerializeObject(error);
			var deserializedError = JsonConvert.DeserializeObject<Error>(item);
			_errors.Add(deserializedError);
			_errorTaskCompletionSource.TrySetResult(deserializedError);
		}

		public void QueueMetrics(IMetricSet metrics) { }

		public void QueueSpan(ISpan span) { }

		public void QueueTransaction(ITransaction transaction)
		{
			var item = _payloadItemSerializer.SerializeObject(transaction);
			var deserializedTransaction = JsonConvert.DeserializeObject<Transaction>(item);
			_transactions.Add(deserializedTransaction);

			_transactionTaskCompletionSource.TrySetResult(transaction);
		}
	}
}
