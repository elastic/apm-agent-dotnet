// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Tests.Mocks
{
	internal class MockPayloadSender : IPayloadSender
	{
		private readonly List<IError> _errors = new List<IError>();
		private readonly object _lock = new object();
		private readonly List<IMetricSet> _metrics = new List<IMetricSet>();
		private readonly List<Func<ISpan, ISpan>> _spanFilters = new List<Func<ISpan, ISpan>>();
		private readonly List<ISpan> _spans = new List<ISpan>();
		private readonly List<Func<ITransaction, ITransaction>> _transactionFilters = new List<Func<ITransaction, ITransaction>>();
		private readonly List<ITransaction> _transactions = new List<ITransaction>();

		public MockPayloadSender(IApmLogger logger = null)
			=> PayloadSenderV2.SetUpFilters(_transactionFilters, _spanFilters, new MockConfigSnapshot(),
				new MockApmServerInfo(new ElasticVersion(7, 10, 0, null)), logger ?? new NoopLogger());

		private TaskCompletionSource<ITransaction> _transactionTaskCompletionSource = new TaskCompletionSource<ITransaction>();

		public IReadOnlyList<IError> Errors => CreateImmutableSnapshot(_errors);

		public Error FirstError => _errors.First() as Error;

		public MetricSet FirstMetric => _metrics.First() as MetricSet;

		/// <summary>
		/// The 1. Span on the 1. Transaction
		/// </summary>
		public Span FirstSpan => _spans.First() as Span;

		public Transaction FirstTransaction =>
			Transactions.First() as Transaction;

		public IReadOnlyList<IMetricSet> Metrics => CreateImmutableSnapshot(_metrics);

		public IReadOnlyList<ISpan> Spans => CreateImmutableSnapshot(_spans);

		public Span[] SpansOnFirstTransaction =>
			_spans.Where(n => n.TransactionId == Transactions.First().Id).Select(n => n as Span).ToArray();

		public IReadOnlyList<ITransaction> Transactions
		{
			get
			{
				var timer = new Timer { Interval = 1000 };

				timer.Enabled = true;
				timer.Start();

				timer.Elapsed += (a, b) =>
				{
					_transactionTaskCompletionSource.TrySetCanceled();
					timer.Stop();
				};

				try
				{
					_transactionTaskCompletionSource.Task.Wait();
				}
				catch
				{
					if (_transactions != null)
						return CreateImmutableSnapshot(_transactions);

					return new List<ITransaction>();
				}

				return CreateImmutableSnapshot(_transactions);
			}
		}

		internal void ResetTransactionTaskCompletionSource() => _transactionTaskCompletionSource = new TaskCompletionSource<ITransaction>();

		public void QueueError(IError error) => _errors.Add(error);

		public virtual void QueueTransaction(ITransaction transaction)
		{
			transaction = _transactionFilters.Aggregate(transaction, (current, filter) => filter(current));
			_transactions.Add(transaction);
			_transactionTaskCompletionSource.TrySetResult(transaction);
		}

		public void QueueSpan(ISpan span)
		{
			lock (_lock)
			{
				span = _spanFilters.Aggregate(span, (current, filter) => filter(current));
				_spans.Add(span);
			}
		}

		public void QueueMetrics(IMetricSet metricSet) => _metrics.Add(metricSet);

		public void Clear()
		{
			_spans.Clear();
			_errors.Clear();
			_transactions.Clear();
			_metrics.Clear();
		}

		private static IReadOnlyList<T> CreateImmutableSnapshot<T>(IEnumerable<T> source) => new List<T>(source);
	}
}
