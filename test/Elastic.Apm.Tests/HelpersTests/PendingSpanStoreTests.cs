// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Moq;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class PendingSpanStoreTests
	{
		private long _now;

		private PendingSpanStore CreateStore(int maxEntries = PendingSpanStore.DefaultMaxEntries, TimeSpan? sweepInterval = null) =>
			new(new NoopLogger(), maxEntries, sweepInterval, () => _now);

		private static long Ticks(TimeSpan timeSpan) => (long)(timeSpan.TotalSeconds * Stopwatch.Frequency);

		private void Advance(TimeSpan timeSpan) => _now += Ticks(timeSpan);

		private static ISpan NewSpan() => new Mock<ISpan>().Object;

		[Fact]
		public void Add_Then_TryRemove_ReturnsEntry()
		{
			using var store = CreateStore();
			var key = Guid.NewGuid();
			var span = NewSpan();

			store.Add(key, span);
			store.Count.Should().Be(1);

			store.TryRemove(key, out var removed).Should().BeTrue();
			removed.Should().BeSameAs(span);
			store.Count.Should().Be(0);
			store.TryRemove(key, out _).Should().BeFalse();
		}

		[Fact]
		public void Add_WithNullSpan_IsIgnored()
		{
			using var store = CreateStore();

			store.Add(Guid.NewGuid(), null);

			store.Count.Should().Be(0);
			store.TryRemove(Guid.NewGuid(), out _).Should().BeFalse();
		}

		[Fact]
		public void ExpiredEntries_AreEvicted_OnSweep()
		{
			// a zero sweep interval sweeps on every Add
			using var store = CreateStore(sweepInterval: TimeSpan.Zero);
			var expiredKey = Guid.NewGuid();
			var freshKey = Guid.NewGuid();

			store.Add(expiredKey, NewSpan());
			Advance(PendingSpanStore.DefaultMaxAge + TimeSpan.FromMinutes(1));
			store.Add(freshKey, NewSpan());

			store.Count.Should().Be(1);
			store.TryRemove(expiredKey, out _).Should().BeFalse();
			store.TryRemove(freshKey, out _).Should().BeTrue();
		}

		[Fact]
		public void MaxAge_IsFlooredToDefault()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.Zero);
			var key = Guid.NewGuid();

			store.Add(key, NewSpan(), TimeSpan.FromSeconds(1));

			// well past the requested age but below the floor - must survive
			Advance(TimeSpan.FromMinutes(5));
			store.Add(Guid.NewGuid(), NewSpan());
			store.Count.Should().Be(2);

			// past the floor - must be evicted
			Advance(PendingSpanStore.DefaultMaxAge);
			store.Add(Guid.NewGuid(), NewSpan());
			store.TryRemove(key, out _).Should().BeFalse();
		}

		[Fact]
		public void InfiniteMaxAge_IsNotTimeEvicted()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.Zero);
			var key = Guid.NewGuid();

			store.Add(key, NewSpan(), TimeSpan.MaxValue);

			Advance(TimeSpan.FromDays(365));
			store.Sweep();

			store.TryRemove(key, out _).Should().BeTrue();
		}

		[Fact]
		public void ExceedingMaxEntries_EvictsOldestEntries()
		{
			const int maxEntries = 10;
			// a long sweep interval ensures only the cap can trigger a sweep
			using var store = CreateStore(maxEntries, TimeSpan.FromDays(1));

			var keys = new Guid[maxEntries + 1];
			for (var i = 0; i < keys.Length; i++)
			{
				keys[i] = Guid.NewGuid();
				Advance(TimeSpan.FromMilliseconds(1));
				store.Add(keys[i], NewSpan());
			}

			store.Count.Should().Be(maxEntries);
			// the oldest entry was evicted; all newer entries survived
			store.TryRemove(keys[0], out _).Should().BeFalse();
			for (var i = 1; i < keys.Length; i++)
				store.TryRemove(keys[i], out _).Should().BeTrue();
		}

		[Fact]
		public void SweepInterval_GatesTimeBasedEviction()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.FromHours(2));
			var key = Guid.NewGuid();

			store.Add(key, NewSpan());

			// Entry is past its max age, but no sweep is due yet. A late completion event must still find it
			Advance(PendingSpanStore.DefaultMaxAge + TimeSpan.FromMinutes(1));
			store.Add(Guid.NewGuid(), NewSpan());

			store.Count.Should().Be(2);
			store.TryRemove(key, out _).Should().BeTrue();
		}

		[Fact]
		public void Timer_EvictsExpiredEntries_WithoutAnotherAdd()
		{
			using var store = new PendingSpanStore(new NoopLogger(), sweepInterval: TimeSpan.FromMilliseconds(10),
				minimumMaxAge: TimeSpan.FromMilliseconds(20));

			store.Add(Guid.NewGuid(), NewSpan(), TimeSpan.FromMilliseconds(20));

			SpinWait.SpinUntil(() => store.Count == 0, TimeSpan.FromSeconds(5)).Should().BeTrue();
		}

		[Fact]
		public void Dispose_ClearsEntries_AndPreventsFurtherAdds()
		{
			var store = CreateStore();
			store.Add(Guid.NewGuid(), NewSpan());

			store.Dispose();
			store.Count.Should().Be(0);

			store.Add(Guid.NewGuid(), NewSpan());
			store.Count.Should().Be(0);
		}

		[Fact]
		public void DuplicateKey_KeepsExistingEntry_AndDoesNotIncreaseCount()
		{
			using var store = CreateStore();
			var key = Guid.NewGuid();
			var first = NewSpan();
			var second = NewSpan();

			store.Add(key, first);
			store.Add(key, second);

			store.Count.Should().Be(1);
			store.TryRemove(key, out var removed).Should().BeTrue();
			removed.Should().BeSameAs(first);
			store.Count.Should().Be(0);
		}

		[Fact]
		public void TryRemove_AfterEviction_ReturnsFalse()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.Zero);
			var key = Guid.NewGuid();
			var span = NewSpan();

			store.Add(key, span);
			Advance(PendingSpanStore.DefaultMaxAge + TimeSpan.FromMinutes(1));
			store.Sweep();

			store.TryRemove(key, out var removed).Should().BeFalse();
			removed.Should().BeNull();
			store.Count.Should().Be(0);
		}

		[Fact]
		public void ConcurrentAddsAndRemoves_KeepAccurateCount()
		{
			using var store = CreateStore(maxEntries: 20_000, sweepInterval: TimeSpan.FromDays(1));
			var keys = new Guid[10_000];
			for (var i = 0; i < keys.Length; i++)
				keys[i] = Guid.NewGuid();

			Parallel.For(0, keys.Length, i => store.Add(keys[i], NewSpan()));
			store.Count.Should().Be(keys.Length);

			Parallel.For(0, keys.Length, i => store.TryRemove(keys[i], out _));
			store.Count.Should().Be(0);
		}

		[Fact]
		public void ConcurrentSweepAndRemove_DoesNotCorruptCount()
		{
			using var store = CreateStore(maxEntries: 5_000, sweepInterval: TimeSpan.Zero);
			var keys = new Guid[1_000];
			for (var i = 0; i < keys.Length; i++)
			{
				keys[i] = Guid.NewGuid();
				store.Add(keys[i], NewSpan());
			}

			Advance(PendingSpanStore.DefaultMaxAge + TimeSpan.FromMinutes(1));

			Parallel.Invoke(
				() => store.Sweep(),
				() => Parallel.For(0, keys.Length, i => store.TryRemove(keys[i], out _)),
				() => store.Sweep());

			store.Count.Should().Be(0);
			foreach (var key in keys)
				store.TryRemove(key, out _).Should().BeFalse();
		}

		[Fact]
		public void ConcurrentAdds_UnderCapPressure_StayWithinBound()
		{
			const int maxEntries = 100;
			using var store = CreateStore(maxEntries, TimeSpan.FromDays(1));

			Parallel.For(0, maxEntries * 10, _ =>
			{
				Interlocked.Add(ref _now, Ticks(TimeSpan.FromMilliseconds(1)));
				store.Add(Guid.NewGuid(), NewSpan());
			});

			// Concurrent Adds can briefly overshoot the cap until a sweep completes; a final sweep
			// must bring the store back within the bound.
			store.Sweep();
			store.Count.Should().BeLessOrEqualTo(maxEntries);
		}

		[Fact]
		public void TransactionEnding_RemovesAndAbandonsTrackedSpan()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.FromDays(1));
			var payloadSender = new MockPayloadSender();
			using var agent = new ApmAgent(new TestAgentComponents(payloadSender: payloadSender));
			var key = Guid.NewGuid();

			agent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				var transaction = (Transaction)t;
				var span = transaction.StartSpanInternal("db", "db", makeCurrent: false);
				store.Add(key, span);
				store.Count.Should().Be(1);
				transaction.ChildDurationTimer.ActiveChildren.Should().Be(1);
			});

			store.Count.Should().Be(0);
			store.TryRemove(key, out _).Should().BeFalse();
			payloadSender.Spans.Should().BeEmpty();
			payloadSender.WaitForTransactions();
			payloadSender.FirstTransaction.SelfDuration.Should().Be(payloadSender.FirstTransaction.Duration!.Value);
		}

		[Fact]
		public void RemovingEntry_UnregistersTransactionEndingHandler()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.FromDays(1));
			using var agent = new ApmAgent(new TestAgentComponents());

			agent.Tracer.CaptureTransaction("transaction", "type", t =>
			{
				var transaction = (Transaction)t;
				var span = transaction.StartSpanInternal("db", "db", makeCurrent: false);
				var key = Guid.NewGuid();
				store.Add(key, span);

				transaction.EndingHandlerCount.Should().Be(1);
				store.TryRemove(key, out var removed).Should().BeTrue();
				removed.Should().BeSameAs(span);
				transaction.EndingHandlerCount.Should().Be(0);
				span.Abandon();
			});
		}

		[Fact]
		public void AddAfterTransactionHasStartedEnding_AbandonsSpanImmediately()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.FromDays(1));
			using var agent = new ApmAgent(new TestAgentComponents());
			var transaction = (Transaction)agent.Tracer.StartTransaction("transaction", "type");
			transaction.End();
			var span = transaction.StartSpanInternal("db", "db", makeCurrent: false);

			store.Add(Guid.NewGuid(), span);

			store.Count.Should().Be(0);
			transaction.EndingHandlerCount.Should().Be(0);
		}

		[Fact]
		public void Eviction_ReleasesSpanReference()
		{
			using var store = CreateStore(sweepInterval: TimeSpan.Zero);
			var weakReference = AddExpiredSpan(store);

			ForceGarbageCollection();

			weakReference.IsAlive.Should().BeFalse();
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private WeakReference AddExpiredSpan(PendingSpanStore store)
		{
			var span = NewSpan();
			var weakReference = new WeakReference(span);
			store.Add(Guid.NewGuid(), span);
			Advance(PendingSpanStore.DefaultMaxAge + TimeSpan.FromMinutes(1));
			store.Sweep();
			return weakReference;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void ForceGarbageCollection()
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}
	}
}
