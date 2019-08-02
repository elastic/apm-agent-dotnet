using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class LazyContextualInitTests
	{
		[Theory]
		[MemberData(nameof(WaysToCallInitOrGetString))]
		internal void InitializedOnlyOnceOnFirstAccess(string dbgWayToCallDesc, Func<LazyContextualInit<string>, Func<string>, string> wayToCall)
		{
			var counter = new ThreadSafeCounter();
			var ctxLazy = new LazyContextualInit<string>();
			ctxLazy.IsInited.Should().BeFalse();
			ctxLazy.IfNotInited.Should().NotBeNull();
			var val1 = wayToCall(ctxLazy, () => counter.Increment().ToString());
			ctxLazy.IsInited.Should().BeTrue($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			ctxLazy.IfNotInited.Should().BeNull();
			val1.Should().Be("1");
			counter.Value.Should().Be(1);

			var val2 = wayToCall(ctxLazy, () => counter.Increment().ToString());
			ctxLazy.IsInited.Should().BeTrue();
			val2.Should().Be("1");
			counter.Value.Should().Be(1);
		}

		[Theory]
		[MemberData(nameof(WaysToCallInitOrGetString))]
		internal void InitializedOnlyOnceFromMultipleThreads(string dbgWayToCallDesc, Func<LazyContextualInit<string>, Func<string>, string> wayToCall
		)
		{
			var counter = new ThreadSafeCounter();
			var ctxLazy = new LazyContextualInit<string>();
			var numberOfThreads = Math.Max(Environment.ProcessorCount, 2);
			var threadIndexesAndValues = new ConcurrentBag<ValueTuple<int, string>>();
			var barrier = new Barrier(numberOfThreads);
			var expectedThreadIndexes = Enumerable.Range(1, numberOfThreads);
			var threads = expectedThreadIndexes.Select(i => new Thread(() => EachThreadDo(i))).ToList();
			foreach (var thread in threads) thread.Start();
			foreach (var thread in threads) thread.Join();

			ctxLazy.IsInited.Should().BeTrue($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			counter.Value.Should().Be(1);
			threadIndexesAndValues.Should().HaveCount(numberOfThreads);
			var actualThreadIndexes = new HashSet<int>();
			foreach (var (threadIndex, value) in threadIndexesAndValues)
			{
				value.Should().Be("1");
				actualThreadIndexes.Add(threadIndex);
			}
			actualThreadIndexes.Should().HaveCount(numberOfThreads);
			foreach (var expectedThreadIndex in expectedThreadIndexes) actualThreadIndexes.Should().Contain(expectedThreadIndex);

			void EachThreadDo(int threadIndex)
			{
				barrier.SignalAndWait();
				var value = wayToCall(ctxLazy, () => counter.Increment().ToString());
				threadIndexesAndValues.Add((threadIndex, value));
			}
		}

		// ReSharper disable once MemberCanBeProtected.Global
		public static IEnumerable<object[]> WaysToCallInitOrGet<T>() where T : class
		{
			ValueTuple<string, Func<LazyContextualInit<T>, Func<T>, T>>[] waysToCall =
			{
				("IfNotInited?.InitOrGet ?? Value",
					(lazyValue, valueProducer) => lazyValue.IfNotInited?.InitOrGet(valueProducer) ?? lazyValue.Value),
				("InitOrGet", (lazyValue, valueProducer) => lazyValue.InitOrGet(valueProducer))
			};

			foreach (var (dbgWayToCallDesc, wayToCall) in waysToCall) yield return new object[] { dbgWayToCallDesc, wayToCall };
		}

		public static IEnumerable<object[]> WaysToCallInitOrGetString() => WaysToCallInitOrGet<string>();

		private class ThreadSafeCounter
		{
			internal ThreadSafeCounter(int initialValue = 0) => _value = initialValue;

			private int _value;

			internal int Value => Interlocked.CompareExchange(ref _value, 0, 0);

			internal int Increment() => Interlocked.Increment(ref _value);
		}
	}
}
