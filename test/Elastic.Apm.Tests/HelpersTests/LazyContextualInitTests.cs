using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.TestHelpers;
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
			var counter = new ThreadSafeIntCounter();
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
		internal void multiple_threads(string dbgWayToCallDesc, Func<LazyContextualInit<string>, Func<string>, string> wayToCall)
		{
			var counter = new ThreadSafeIntCounter();
			var ctxLazy = new LazyContextualInit<string>();

			var threadResults = MultiThreadsTestUtils.TestOnThreads(threadIndex =>
			{
				return wayToCall(ctxLazy, () => counter.Increment().ToString());
			});

			ctxLazy.IsInited.Should().BeTrue($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			counter.Value.Should().Be(1);

			threadResults.ForEach(x => x.Should().Be("1"));
		}

		// ReSharper disable once MemberCanBeProtected.Global
		public static IEnumerable<object[]> WaysToCallInitOrGet<T>() where T: class
		{
			ValueTuple<string, Func<LazyContextualInit<T>, Func<T>, T>>[] waysToCall =
			{
				("IfNotInited?.InitOrGet ?? Value", (lazyValue, valueProducer) => lazyValue.IfNotInited?.InitOrGet(valueProducer) ?? lazyValue.Value),
				("InitOrGet", (lazyValue, valueProducer) => lazyValue.InitOrGet(valueProducer))
			};

			foreach (var (dbgWayToCallDesc, wayToCall) in waysToCall) yield return new object[] { dbgWayToCallDesc, wayToCall };
		}

		public static IEnumerable<object[]> WaysToCallInitOrGetString() => WaysToCallInitOrGet<string>();
	}
}
