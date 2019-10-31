using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Model;
using Elastic.Apm.Report;

namespace Elastic.Apm.Tests.Mocks
{
	internal class MockPayloadSender : IPayloadSender
	{
		private readonly List<IError> _errors = new List<IError>();
		private readonly object _lock = new object();
		private readonly List<IMetricSet> _metrics = new List<IMetricSet>();
		private readonly List<ISpan> _spans = new List<ISpan>();
		private readonly List<ITransaction> _transactions = new List<ITransaction>();

		public IReadOnlyList<IError> Errors => DoUnderLock(() => CreateImmutableSnapshot(_errors));

		public Error FirstError => DoUnderLock(() => _errors.First() as Error);

		/// <summary>
		/// The 1. Span on the 1. Transaction
		/// </summary>
		public Span FirstSpan => DoUnderLock(() => _spans.First() as Span);

		public Transaction FirstTransaction => DoUnderLock(() => _transactions.First() as Transaction);
		public IReadOnlyList<IMetricSet> Metrics => DoUnderLock(() => CreateImmutableSnapshot(_metrics));
		public IReadOnlyList<ISpan> Spans => DoUnderLock(() => CreateImmutableSnapshot(_spans));

		public Span[] SpansOnFirstTransaction =>
			DoUnderLock(() => _spans.Where(n => n.TransactionId == _transactions.First().Id).Select(n => n as Span).ToArray());

		public IReadOnlyList<ITransaction> Transactions => DoUnderLock(() => CreateImmutableSnapshot(_transactions));

		public void QueueError(IError error) => DoUnderLock(() => { _errors.Add(error); });

		public void QueueTransaction(ITransaction transaction) => DoUnderLock(() => { _transactions.Add(transaction); });

		public void QueueSpan(ISpan span) => DoUnderLock(() => { _spans.Add(span); });

		public void QueueMetrics(IMetricSet metricSet) => DoUnderLock(() => { _metrics.Add(metricSet); });

		public void Clear() => DoUnderLock(() =>
		{
			_spans.Clear();
			_errors.Clear();
			_transactions.Clear();
			_metrics.Clear();
		});

		private TResult DoUnderLock<TResult>(Func<TResult> func)
		{
			lock (_lock) return func();
		}

		private void DoUnderLock(Action action)
		{
			lock (_lock) action();
		}

		private static IReadOnlyList<T> CreateImmutableSnapshot<T>(IEnumerable<T> source) => new List<T>(source);
	}
}
