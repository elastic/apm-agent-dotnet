// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class LazyContextualInitTests
	{
		public static readonly TheoryData WaysToCallInit = new TheoryData<string, Func<LazyContextualInit, Action, bool>>()
		{
			{ "IfNotInited?.Init ?? false", (lazyCtxInit, initAction) => lazyCtxInit.IfNotInited?.Init(initAction) ?? false },
			{ "Init", (lazyCtxInit, initAction) => lazyCtxInit.Init(initAction) }
		};

		[Theory]
		[MemberData(nameof(WaysToCallInitOrGetString))]
		internal void with_result_initialized_only_once_on_first_call(string dbgWayToCallDesc,
			Func<LazyContextualInit<string>, Func<string>, string> wayToCall
		)
		{
			var counter = new ThreadSafeIntCounter();
			var lazyCtxInit = new LazyContextualInit<string>();
			lazyCtxInit.IsInited.Should().BeFalse();
			lazyCtxInit.IfNotInited.Should().NotBeNull();
			var val1 = wayToCall(lazyCtxInit, () => counter.Increment().ToString());
			lazyCtxInit.IsInited.Should().BeTrue($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			lazyCtxInit.IfNotInited.Should().BeNull();
			val1.Should().Be("1");
			counter.Value.Should().Be(1);

			var val2 = wayToCall(lazyCtxInit, () => counter.Increment().ToString());
			lazyCtxInit.IsInited.Should().BeTrue();
			val2.Should().Be("1");
			counter.Value.Should().Be(1);
		}

		[Theory]
		[MemberData(nameof(WaysToCallInitOrGetString))]
		internal void with_result_multiple_threads(string dbgWayToCallDesc, Func<LazyContextualInit<string>, Func<string>, string> wayToCall)
		{
			var counter = new ThreadSafeIntCounter();
			var lazyCtxInit = new LazyContextualInit<string>();

			var threadResults = MultiThreadsTestUtils.TestOnThreads(_ => wayToCall(lazyCtxInit, () => counter.Increment().ToString()));

			lazyCtxInit.IsInited.Should().BeTrue($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			counter.Value.Should().Be(1);

			threadResults.ForEach(x => x.Should().Be("1"));
		}

		[Theory]
#pragma warning disable xUnit1037 // There are fewer theory data type arguments than required by the parameters of the test method
		[MemberData(nameof(WaysToCallInit))]
#pragma warning restore xUnit1037 // There are fewer theory data type arguments than required by the parameters of the test method
		internal void no_result_initialized_only_once_on_first_call(string dbgWayToCallDesc, Func<LazyContextualInit, Action, bool> wayToCall)
		{
			var counter = new ThreadSafeIntCounter();
			var lazyCtxInit = new LazyContextualInit();
			lazyCtxInit.IsInited.Should().BeFalse();
			lazyCtxInit.IfNotInited.Should().NotBeNull();

			var isInitedByThisCall = wayToCall(lazyCtxInit, () => counter.Increment());

			isInitedByThisCall.Should().BeTrue($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			lazyCtxInit.IsInited.Should().BeTrue();
			lazyCtxInit.IfNotInited.Should().BeNull();
			counter.Value.Should().Be(1);

			isInitedByThisCall = wayToCall(lazyCtxInit, () => counter.Increment());

			isInitedByThisCall.Should().BeFalse($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			lazyCtxInit.IsInited.Should().BeTrue();
			counter.Value.Should().Be(1);
		}

		[Theory]
#pragma warning disable xUnit1037 // There are fewer theory data type arguments than required by the parameters of the test method
		[MemberData(nameof(WaysToCallInit))]
#pragma warning restore xUnit1037 // There are fewer theory data type arguments than required by the parameters of the test method
		internal void no_result_multiple_threads(string dbgWayToCallDesc, Func<LazyContextualInit, Action, bool> wayToCall)
		{
			var counter = new ThreadSafeIntCounter();
			var lazyCtxInit = new LazyContextualInit();

			var threadResults = MultiThreadsTestUtils.TestOnThreads(_ => wayToCall(lazyCtxInit, () => counter.Increment()));

			lazyCtxInit.IsInited.Should().BeTrue($"{nameof(dbgWayToCallDesc)}: {dbgWayToCallDesc}");
			counter.Value.Should().Be(1);

			threadResults.Where(isInitedByThisCall => isInitedByThisCall).Should().ContainSingle();
			threadResults.Where(isInitedByThisCall => !isInitedByThisCall).Should().HaveCount(threadResults.Count - 1);
		}

		public static TheoryData WaysToCallInitOrGet<T>() where T : class =>
			new TheoryData<string, Func<LazyContextualInit<T>, Func<T>, T>>
			{
				{
					"IfNotInited?.InitOrGet ?? Value",
					(lazyCtxInit, valueProducer) => lazyCtxInit.IfNotInited?.InitOrGet(valueProducer) ?? lazyCtxInit.Value
				},
				{ "InitOrGet", (lazyCtxInit, valueProducer) => lazyCtxInit.InitOrGet(valueProducer) }
			};

		public static IEnumerable<object[]> WaysToCallInitOrGetString() => WaysToCallInitOrGet<string>();
	}
}
