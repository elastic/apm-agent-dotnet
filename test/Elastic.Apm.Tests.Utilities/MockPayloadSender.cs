// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Ingest;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report;
using Elastic.Transport;
using FluentAssertions;

#nullable enable
namespace Elastic.Apm.Tests.Utilities
{
	internal class MockPayloadSender : ApmChannel
	{
		private static readonly JObject JsonSpanTypesData =
			JObject.Parse(File.ReadAllText("./TestResources/json-specs/span_types.json"));

		private readonly object _spanLock = new();
		private readonly object _transactionLock = new();
		private readonly object _metricsLock = new();
		private readonly object _errorLock = new();
		private readonly List<IMetricSet> _metrics = new();
		private readonly List<IError> _errors = new();
		private readonly List<ISpan> _spans = new();
		private readonly List<ITransaction> _transactions = new();

		public MockPayloadSender(IApmLogger? logger = null)
			: base(new ApmChannelOptions(new Uri("http://localhost:8080"), transportClient: new InMemoryConnection()), logger)
		{
			_waitHandles = new[] { new AutoResetEvent(false), new AutoResetEvent(false), new AutoResetEvent(false), new AutoResetEvent(false) };

			_transactionWaitHandle = _waitHandles[0];
			_spanWaitHandle = _waitHandles[1];
			_errorWaitHandle = _waitHandles[2];
			_metricSetWaitHandle = _waitHandles[3];

		}

		/// <summary>
		/// Allows opt-in to strict span type/sub-type checking
		/// TODO: In the future this should become an opt-out setting.
		/// </summary>
		public bool IsStrictSpanCheckEnabled { get; set; } = false;

		private readonly AutoResetEvent _transactionWaitHandle;
		private readonly AutoResetEvent _spanWaitHandle;
		private readonly AutoResetEvent _errorWaitHandle;
		private readonly AutoResetEvent _metricSetWaitHandle;
		private readonly AutoResetEvent[] _waitHandles;
		private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

		public override bool TryWrite(IIntakeRoot item)
		{
			var written = base.TryWrite(item);
			switch (item)
			{
				case IError error:
					_errors.Add(error);
					_errorWaitHandle.Set();
					break;
				case ITransaction transaction:
					_transactions.Add(transaction);
					_transactionWaitHandle.Set();
					break;
				case ISpan span:
					_spans.Add(span);
					_spanWaitHandle.Set();
					break;
				case IMetricSet metricSet:
					_metrics.Add(metricSet);
					_metricSetWaitHandle.Set();
					break;
			}
			return written;
		}

		public override void QueueError(IError error)
		{
			lock (_errorLock)
				base.QueueError(error);
		}

		public override void QueueTransaction(ITransaction transaction)
		{
			lock (_transactionLock)
				base.QueueTransaction(transaction);
		}

		public override void QueueSpan(ISpan span)
		{
			VerifySpan(span);
			lock (_spanLock)
				base.QueueSpan(span);
		}

		public override void QueueMetrics(IMetricSet metricSet)
		{
			lock (_metricsLock)
				base.QueueMetrics(metricSet);
		}



		/// <summary>
		/// Waits for any events to be queued
		/// </summary>
		/// <param name="timeout">Optional timeout to wait. If unspecified, will wait up to <see cref="DefaultTimeout"/></param>
		public void WaitForAny(TimeSpan? timeout = null) =>
			WaitHandle.WaitAny(_waitHandles, timeout ?? DefaultTimeout);

		/// <summary>
		/// Waits for transactions to be queued
		/// </summary>
		/// <param name="timeout">Optional timeout to wait. If unspecified, will wait up to <see cref="DefaultTimeout"/></param>
		/// <param name="count"></param>
		/// <returns><c>true</c> if the event was signalled, <c>false</c> otherwise.</returns>
		public bool WaitForTransactions(TimeSpan? timeout = null, int? count = null)
		{
			if (count != null)
			{
				int transactionCount;
				var signalled = true;
				if (timeout is null)
				{
					lock (_transactionLock)
						transactionCount = _transactions.Count;
					while (transactionCount < count && signalled)
					{
						signalled = _transactionWaitHandle.WaitOne(DefaultTimeout);
						lock (_transactionLock)
							transactionCount = _transactions.Count;
					}
				}
				else
				{
					var stopWatch = Stopwatch.StartNew();

					lock (_transactionLock)
						transactionCount = _transactions.Count;

					while (transactionCount < count && signalled)
					{
						var elapsedMilliseconds = Convert.ToInt32(timeout.Value.TotalMilliseconds - stopWatch.ElapsedMilliseconds);
						signalled = _transactionWaitHandle.WaitOne(elapsedMilliseconds);
						lock (_transactionLock)
							transactionCount = _transactions.Count;
					}
				}

				return signalled;
			}

			return _transactionWaitHandle.WaitOne(timeout ?? DefaultTimeout);
		}

		/// <summary>
		/// Waits for spans to be queued
		/// </summary>
		/// <param name="timeout">Optional timeout to wait. If unspecified, will wait up to <see cref="DefaultTimeout"/></param>
		/// <returns><c>true</c> if the event was signalled, <c>false</c> otherwise.</returns>
		public bool WaitForSpans(TimeSpan? timeout = null, int? count = null)
		{
			if (count != null)
			{
				int spanCount;
				var signalled = true;
				if (timeout is null)
				{
					lock (_spanLock)
						spanCount = _spans.Count;
					while (spanCount < count && signalled)
					{
						signalled = _spanWaitHandle.WaitOne(DefaultTimeout);
						lock (_spanLock)
							spanCount = _spans.Count;
					}
				}
				else
				{
					var stopWatch = Stopwatch.StartNew();

					lock (_spanLock)
						spanCount = _spans.Count;
					while (spanCount < count && signalled)
					{
						var elapsedMilliseconds = Convert.ToInt32(timeout.Value.TotalMilliseconds - stopWatch.ElapsedMilliseconds);
						signalled = _spanWaitHandle.WaitOne(elapsedMilliseconds);
						lock (_spanLock)
							spanCount = _spans.Count;
					}
				}

				return signalled;
			}

			return _spanWaitHandle.WaitOne(timeout ?? DefaultTimeout);
		}

		/// <summary>
		/// Waits for errors to be queued
		/// </summary>
		/// <param name="timeout">Optional timeout to wait. If unspecified, will wait up to <see cref="DefaultTimeout"/></param>
		/// <returns><c>true</c> if the event was signalled, <c>false</c> otherwise.</returns>
		public bool WaitForErrors(TimeSpan? timeout = null) =>
			_errorWaitHandle.WaitOne(timeout ?? DefaultTimeout);

		/// <summary>
		/// Waits for metrics to be queued
		/// </summary>
		/// <param name="timeout">Optional timeout to wait. If unspecified, will wait up to <see cref="DefaultTimeout"/></param>
		/// <returns><c>true</c> if the event was signalled, <c>false</c> otherwise.</returns>
		public bool WaitForMetrics(TimeSpan? timeout = null) =>
			_metricSetWaitHandle.WaitOne(timeout ?? DefaultTimeout);

		/// <summary>
		/// Sets transaction wait handle to signalled, allowing threads to proceed.
		/// Can be called when making an assertion on the absence of a transaction where
		/// the order of execution is known, to prevent waiting for a given timeout.
		/// </summary>
		public void SignalEndTransactions() => _transactionWaitHandle.Set();

		/// <summary>
		/// Sets spans wait handle to signalled, allowing threads to proceed.
		/// Can be called when making an assertion on the absence of a span where
		/// the order of execution is known, to prevent waiting for a given timeout.
		/// </summary>
		public void SignalEndSpans() => _spanWaitHandle.Set();

		public IReadOnlyList<IError> Errors
		{
			get
			{
				lock (_errorLock)
					return CreateImmutableSnapshot(_errors);
			}
		}

		public Error? FirstError => Errors.FirstOrDefault() as Error;
		public MetricSet? FirstMetric => Metrics.FirstOrDefault() as MetricSet;

		/// <summary>
		/// The 1. Span on the 1. Transaction
		/// </summary>
		public Span? FirstSpan => Spans.FirstOrDefault() as Span;

		public Transaction? FirstTransaction =>
			Transactions.FirstOrDefault() as Transaction;

		public IReadOnlyList<IMetricSet> Metrics
		{
			get
			{
				lock (_metricsLock)
					return CreateImmutableSnapshot(_metrics);
			}
		}

		public IReadOnlyList<ISpan> Spans
		{
			get
			{
				lock (_spanLock)
					return CreateImmutableSnapshot(_spans);
			}
		}

		public IReadOnlyList<ITransaction> Transactions
		{
			get
			{
				lock (_transactionLock)
					return CreateImmutableSnapshot(_transactions);
			}
		}

		public Span[] SpansOnFirstTransaction =>
			Spans
				.Where(n => n.TransactionId == Transactions.First().Id)
				.Select(n => (Span)n)
				.ToArray();

		private void VerifySpan(ISpan span)
		{
			var type = span.Type;
			type.Should().NotBeNullOrEmpty("span type is mandatory");

			if (IsStrictSpanCheckEnabled)
			{
				var spanTypeInfo = JsonSpanTypesData[type] as JObject;
				spanTypeInfo.Should().NotBeNull($"span type '{type}' is not allowed by the spec");

				var allowNullSubtype = spanTypeInfo!["allow_null_subtype"]?.Value<bool>();
				var allowUnlistedSubtype = spanTypeInfo["allow_unlisted_subtype"]?.Value<bool>();
				var subTypes = spanTypeInfo["subtypes"];
				var hasSubtypes = subTypes != null && subTypes.Any();

				var subType = span.Subtype;
				if (subType != null)
				{
					if (!allowUnlistedSubtype.GetValueOrDefault() && hasSubtypes)
					{
						var subTypeInfo = subTypes![subType];
						subTypeInfo.Should()
							.NotBeNull($"span subtype '{subType}' is not allowed by the spec for type '{type}'");
					}
				}
				else
				{
					if (!hasSubtypes)
					{
						allowNullSubtype.Should().Be(true,
							$"span type '{type}' requires non-null subtype (allow_null_subtype=false)");
					}
				}
			}
		}

		public void Clear()
		{
			lock (_spanLock)
				_spans.Clear();
			lock (_errorLock)
				_errors.Clear();
			lock (_transactionLock)
				_transactions.Clear();
			lock (_metricsLock)
				_metrics.Clear();
		}

		private static IReadOnlyList<T> CreateImmutableSnapshot<T>(IEnumerable<T> source) => new List<T>(source);
	}
}
