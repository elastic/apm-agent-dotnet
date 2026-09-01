// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Model;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests
{
	public class ChildDurationTimerTests
	{
		[Fact]
		public void OutOfOrderOverlappingTimestamps_RecordUnionDuration()
		{
			var timer = new ChildDurationTimer();

			// Intervals [200,300] and [100,250] → union [100,300] = 200ms
			var tokenA = timer.OnChildStart(200_000);
			var tokenB = timer.OnChildStart(100_000);
			timer.OnChildEnd(tokenA, 300_000);
			timer.OnChildEnd(tokenB, 250_000);

			timer.Duration.Should().Be(200);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OutOfOrderNonOverlappingTimestamps_RecordExactUnionDuration()
		{
			var timer = new ChildDurationTimer();

			// Intervals [200,300] and [100,150] are disjoint; union is 150ms, not 200ms
			var tokenA = timer.OnChildStart(200_000);
			var tokenB = timer.OnChildStart(100_000);
			timer.OnChildEnd(tokenA, 300_000);
			timer.OnChildEnd(tokenB, 150_000);

			timer.Duration.Should().Be(150);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void SequentialChildren_AccumulateDistinctIntervals()
		{
			var timer = new ChildDurationTimer();

			var tokenA = timer.OnChildStart(100_000);
			timer.OnChildEnd(tokenA, 200_000);
			var tokenB = timer.OnChildStart(300_000);
			timer.OnChildEnd(tokenB, 400_000);

			timer.Duration.Should().Be(200);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void ChronologicalOverlappingChildren_RecordUnionDuration()
		{
			var timer = new ChildDurationTimer();

			var tokenA = timer.OnChildStart(100_000);
			var tokenB = timer.OnChildStart(150_000);
			timer.OnChildEnd(tokenA, 200_000);
			timer.OnChildEnd(tokenB, 250_000);

			timer.Duration.Should().Be(150);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OnChildAbandoned_Alone_ContributesNoDuration()
		{
			var timer = new ChildDurationTimer();

			var token = timer.OnChildStart(100_000);
			timer.OnChildAbandoned(token);

			timer.Duration.Should().Be(0);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OnChildAbandoned_PreservesSiblingInterval()
		{
			var timer = new ChildDurationTimer();

			var tokenA = timer.OnChildStart(100_000);
			var tokenB = timer.OnChildStart(200_000);
			timer.OnChildEnd(tokenA, 150_000); // completes [100,150]
			timer.OnChildAbandoned(tokenB); // drops the open sibling without adding time to abandon

			timer.Duration.Should().Be(50);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OnChildAbandoned_UnknownToken_IsIgnored()
		{
			var timer = new ChildDurationTimer();

			var token = timer.OnChildStart(100_000);
			timer.OnChildAbandoned(9999); // token 9999 was never issued

			timer.ActiveChildren.Should().Be(1);
			timer.Duration.Should().Be(0);

			timer.OnChildEnd(token, 200_000);
			timer.Duration.Should().Be(100);
		}

		[Fact]
		public void OnChildEnd_UnknownToken_IsIgnored()
		{
			var timer = new ChildDurationTimer();

			var token = timer.OnChildStart(100_000);
			timer.OnChildEnd(9999, 150_000); // token 9999 was never issued

			timer.ActiveChildren.Should().Be(1);
			timer.Duration.Should().Be(0);

			timer.OnChildEnd(token, 200_000);
			timer.Duration.Should().Be(100);
		}

		[Fact]
		public void OnChildEnd_UsesExactToken_WhenStartTimestampsMatch()
		{
			var timer = new ChildDurationTimer();

			var abandonedToken = timer.OnChildStart(100_000);
			var endedToken = timer.OnChildStart(100_000);

			timer.OnChildEnd(endedToken, 200_000);
			timer.OnChildAbandoned(abandonedToken);

			timer.ActiveChildren.Should().Be(0);
			timer.Duration.Should().Be(100);
		}

		[Fact]
		public void OnChildEnd_UsesExactToken_WhenChildrenEndOutOfStartOrder()
		{
			var timer = new ChildDurationTimer();

			var endedToken = timer.OnChildStart(100_000);
			var abandonedToken = timer.OnChildStart(200_000);

			timer.OnChildEnd(endedToken, 300_000);
			timer.OnChildAbandoned(abandonedToken);

			timer.ActiveChildren.Should().Be(0);
			timer.Duration.Should().Be(200);
		}

		[Fact]
		public void OnSpanEnd_ClosesOpenChildren()
		{
			var timer = new ChildDurationTimer();

			timer.OnChildStart(100_000);
			timer.OnChildStart(150_000);
			timer.OnSpanEnd(300_000);

			timer.Duration.Should().Be(200);
			timer.ActiveChildren.Should().Be(0);

			var token = timer.OnChildStart(400_000);
			timer.OnChildEnd(token, 500_000);
			timer.Duration.Should().Be(200);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void EndBeforeStartTimestamp_ClampsToZeroContribution()
		{
			var timer = new ChildDurationTimer();

			var token = timer.OnChildStart(200_000);
			timer.OnChildEnd(token, 100_000);

			timer.Duration.Should().Be(0);
			timer.ActiveChildren.Should().Be(0);
		}
	}
}
