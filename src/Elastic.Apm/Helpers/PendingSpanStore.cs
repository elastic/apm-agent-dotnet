// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;

namespace Elastic.Apm.Helpers
{
	/// <summary>
	/// Tracks spans for in-flight operations correlated by a key, while guarding against unbounded growth when an
	/// operation never produces a completion event. For example, Microsoft.Data.SqlClient can emit
	/// WriteCommandBefore without a matching WriteCommandAfter/WriteCommandError on certain cancellation paths,
	/// which would otherwise leak the span (and the object graph it roots) forever.
	/// <para>
	/// Entries that are not removed within their maximum age are evicted, and the total number of tracked entries
	/// is bounded. Evicted spans are dropped without being ended, so no fabricated durations are reported. Expiration
	/// sweeps run periodically as well as opportunistically during <see cref="Add"/>. When a tracked span is a concrete
	/// <see cref="Span"/>, the store also abandons it when the enclosing transaction starts ending, so cancelled operations
	/// do not keep the transaction graph rooted after the request completes.
	/// </para>
	/// </summary>
	internal sealed class PendingSpanStore : IDisposable
	{
		internal const int DefaultMaxEntries = 10_000;

		internal static readonly TimeSpan DefaultMaxAge = TimeSpan.FromMinutes(10);
		internal static readonly TimeSpan DefaultSweepInterval = TimeSpan.FromSeconds(30);

		private static readonly TimerCallback SweepTimerCallback = state => ((PendingSpanStore)state).SweepFromTimer();

		private readonly Func<long> _clock;
		private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
		private readonly IApmLogger _logger;
		private readonly int _maxEntries;
		private readonly long _minimumMaxAgeTicks;
		private readonly long _sweepIntervalTicks;
		private readonly Timer _timer;
		private int _count;
		private int _disposed;
		private long _lastSweepTicks;
		private int _capWarningLogged;

		/// <summary>
		/// Initialises a new instance of the <see cref="PendingSpanStore"/> class.
		/// </summary>
		/// <param name="logger">Logger used to report evictions.</param>
		/// <param name="maxEntries">Hard cap on the number of tracked entries.</param>
		/// <param name="sweepInterval">Interval between time-based sweeps. A sweep is also forced when the number of
		/// entries exceeds <paramref name="maxEntries"/>.</param>
		/// <param name="clock">Returns the current time in <see cref="Stopwatch" /> ticks. Intended for tests;
		/// defaults to <see cref="Stopwatch.GetTimestamp" />.</param>
		/// <param name="minimumMaxAge">Minimum entry lifetime. Intended for tests; defaults to <see cref="DefaultMaxAge"/>.</param>
		internal PendingSpanStore(IApmLogger logger, int maxEntries = DefaultMaxEntries, TimeSpan? sweepInterval = null,
			Func<long> clock = null, TimeSpan? minimumMaxAge = null)
		{
			if (maxEntries <= 0)
				throw new ArgumentOutOfRangeException(nameof(maxEntries));

			if (sweepInterval.HasValue && sweepInterval.Value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(sweepInterval));

			if (minimumMaxAge.HasValue && minimumMaxAge.Value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(minimumMaxAge));

			var effectiveSweepInterval = sweepInterval ?? DefaultSweepInterval;
			var effectiveMinimumMaxAge = minimumMaxAge ?? DefaultMaxAge;

			_logger = logger;
			_maxEntries = maxEntries;
			_sweepIntervalTicks = ToStopwatchTicks(effectiveSweepInterval);
			_minimumMaxAgeTicks = ToStopwatchTicks(effectiveMinimumMaxAge);
			_clock = clock ?? Stopwatch.GetTimestamp;
			_lastSweepTicks = _clock();

			if (effectiveSweepInterval > TimeSpan.Zero)
			{
				if (ExecutionContext.IsFlowSuppressed())
				{
					_timer = new Timer(SweepTimerCallback, this, effectiveSweepInterval, effectiveSweepInterval);
				}
				else
				{
					using (ExecutionContext.SuppressFlow())
						_timer = new Timer(SweepTimerCallback, this, effectiveSweepInterval, effectiveSweepInterval);
				}
			}
		}

		public int Count => Volatile.Read(ref _count);

		/// <summary>
		/// Starts tracking a span for an in-flight operation.
		/// </summary>
		/// <param name="key">Unique key correlating the operation's start and completion events.</param>
		/// <param name="span">The span to track. <c>null</c> is ignored and not stored.</param>
		/// <param name="maxAge">The maximum time the entry may remain in the store before becoming eligible for
		/// eviction. Values below <see cref="DefaultMaxAge"/> are raised to that value in production;
		/// <see cref="TimeSpan.MaxValue"/> disables time-based eviction, and <c>null</c> uses the default.
		/// Prefer a finite age: with <c>makeCurrent: false</c> this store is often the only strong root for the
		/// span, so unbounded retention pins the enclosing transaction graph.</param>
		public void Add(Guid key, ISpan span, TimeSpan? maxAge = null)
		{
			if (span is null)
				return;

			if (Volatile.Read(ref _disposed) != 0)
			{
				Abandon(span);
				return;
			}

			var now = _clock();

			var effectiveMaxAge = maxAge ?? DefaultMaxAge;
			var maxAgeTicks = effectiveMaxAge == TimeSpan.MaxValue
				? long.MaxValue
				: Math.Max(ToStopwatchTicks(effectiveMaxAge), _minimumMaxAgeTicks);
			var deadlineTicks = maxAgeTicks == long.MaxValue || now > long.MaxValue - maxAgeTicks
				? long.MaxValue
				: now + maxAgeTicks;

			var entry = new Entry(span, now, deadlineTicks);
			if (!TryRegisterTransactionEnding(key, entry))
			{
				Abandon(span);
				return;
			}

			Interlocked.Increment(ref _count);

			if (!_entries.TryAdd(key, entry))
			{
				Interlocked.Decrement(ref _count);
				entry.UnregisterTransactionEnding();
				Abandon(span);
			}
			else if (Volatile.Read(ref _disposed) != 0 || entry.TransactionEnded)
			{
				TryRemoveEntry(new KeyValuePair<Guid, Entry>(key, entry));
			}

			SweepIfNeeded(now);
		}

		private bool TryRegisterTransactionEnding(Guid key, Entry entry)
		{
			if (entry.Span is not Span concreteSpan)
				return true;

			var transaction = concreteSpan.EnclosingTransaction;
			void OnEnding()
			{
				entry.MarkTransactionEnded();
				TryRemoveEntry(new KeyValuePair<Guid, Entry>(key, entry));
			}

			entry.SetTransactionEndingRegistration(transaction, OnEnding);
			return transaction.TryRegisterEndingHandler(OnEnding);
		}

		public bool TryRemove(Guid key, out ISpan span)
		{
			if (_entries.TryRemove(key, out var entry))
			{
				Interlocked.Decrement(ref _count);
				entry.UnregisterTransactionEnding();
				span = entry.Span;
				return true;
			}

			span = null;
			return false;
		}

		public void Dispose()
		{
			if (Interlocked.Exchange(ref _disposed, 1) != 0)
				return;

			_timer?.Dispose();

			foreach (var entry in _entries)
				TryRemoveEntry(entry);
		}

		internal void Sweep() => Sweep(_clock());

		private void SweepIfNeeded(long now)
		{
			if (Volatile.Read(ref _disposed) != 0)
				return;

			var overCap = Count > _maxEntries;
			var lastSweep = Interlocked.Read(ref _lastSweepTicks);

			if (!overCap && now - lastSweep < _sweepIntervalTicks)
				return;

			// Claim the sweep; if another thread claimed it concurrently, let that thread do the work.
			if (Interlocked.CompareExchange(ref _lastSweepTicks, now, lastSweep) != lastSweep)
				return;

			Sweep(now);
		}

		private void SweepFromTimer()
		{
			if (Volatile.Read(ref _disposed) != 0)
				return;

			try
			{
				SweepIfNeeded(_clock());
			}
			catch (Exception ex)
			{
				_logger.Error()?.LogException(ex, "Failed sweeping pending spans");
			}
		}

		private void Sweep(long now)
		{
			if (Volatile.Read(ref _disposed) != 0)
				return;

			var expired = 0;
			foreach (var kvp in _entries)
			{
				if (now >= kvp.Value.DeadlineTicks && TryRemoveEntry(kvp))
					expired++;
			}

			var evictedForCap = 0;
			var overflow = Count - _maxEntries;
			if (overflow > 0)
			{
				if (Interlocked.Exchange(ref _capWarningLogged, 1) == 0)
				{
					_logger.Warning()
						?.Log("The number of pending spans exceeded {MaxEntries}. This indicates either an extreme number of"
							+ " concurrent operations or operations that never produce a completion event. Evicting the oldest"
							+ " pending spans; evicted spans will not be reported.", _maxEntries);
				}

				// Oldest entries are the most likely to be orphaned; live entries are typically removed within
				// their operation's normal completion time.
				foreach (var kvp in _entries.ToArray().OrderBy(e => e.Value.AddedTicks).Take(overflow))
				{
					if (TryRemoveEntry(kvp))
						evictedForCap++;
				}
			}

			if (expired > 0 || evictedForCap > 0)
			{
				_logger.Debug()
					?.Log("Evicted {ExpiredCount} expired and {OverflowCount} overflow pending span(s); {RemainingCount} pending span(s) remain."
						+ " Evicted spans will not be reported.", expired, evictedForCap, Count);
			}
		}

		private bool TryRemoveEntry(KeyValuePair<Guid, Entry> kvp)
		{
			if (!((ICollection<KeyValuePair<Guid, Entry>>)_entries).Remove(kvp))
				return false;

			Interlocked.Decrement(ref _count);
			kvp.Value.UnregisterTransactionEnding();
			Abandon(kvp.Value.Span);
			return true;
		}

		private static void Abandon(ISpan span)
		{
			if (span is Span capturedSpan)
				capturedSpan.Abandon();
		}

		private static long ToStopwatchTicks(TimeSpan timeSpan) => (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);

		private sealed class Entry(ISpan span, long addedTicks, long deadlineTicks)
		{
			private Transaction _transaction;
			private Action _transactionEndingHandler;
			private int _transactionEnded;

			public ISpan Span { get; } = span;
			public long AddedTicks { get; } = addedTicks;
			public long DeadlineTicks { get; } = deadlineTicks;
			public bool TransactionEnded => Volatile.Read(ref _transactionEnded) != 0;

			public void SetTransactionEndingRegistration(Transaction transaction, Action handler)
			{
				_transaction = transaction;
				_transactionEndingHandler = handler;
			}

			public void MarkTransactionEnded() => Volatile.Write(ref _transactionEnded, 1);

			public void UnregisterTransactionEnding()
			{
				if (_transaction != null && _transactionEndingHandler != null)
					_transaction.UnregisterEndingHandler(_transactionEndingHandler);
			}
		}
	}
}
