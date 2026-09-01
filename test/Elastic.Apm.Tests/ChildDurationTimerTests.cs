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
			timer.OnChildStart(200_000);
			timer.OnChildStart(100_000);
			timer.OnChildEnd(300_000);
			timer.OnChildEnd(250_000);

			timer.Duration.Should().Be(200);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OutOfOrderNonOverlappingTimestamps_RecordExactUnionDuration()
		{
			var timer = new ChildDurationTimer();

			// Intervals [200,300] and [100,150] are disjoint; union is 150ms, not 200ms
			timer.OnChildStart(200_000);
			timer.OnChildStart(100_000);
			timer.OnChildEnd(300_000);
			timer.OnChildEnd(150_000);

			timer.Duration.Should().Be(150);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void SequentialChildren_AccumulateDistinctIntervals()
		{
			var timer = new ChildDurationTimer();

			timer.OnChildStart(100_000);
			timer.OnChildEnd(200_000);
			timer.OnChildStart(300_000);
			timer.OnChildEnd(400_000);

			timer.Duration.Should().Be(200);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void ChronologicalOverlappingChildren_RecordUnionDuration()
		{
			var timer = new ChildDurationTimer();

			timer.OnChildStart(100_000);
			timer.OnChildStart(150_000);
			timer.OnChildEnd(200_000);
			timer.OnChildEnd(250_000);

			timer.Duration.Should().Be(150);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OnChildAbandoned_Alone_ContributesNoDuration()
		{
			var timer = new ChildDurationTimer();

			timer.OnChildStart(100_000);
			timer.OnChildAbandoned(100_000);

			timer.Duration.Should().Be(0);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OnChildAbandoned_PreservesSiblingInterval()
		{
			var timer = new ChildDurationTimer();

			timer.OnChildStart(100_000);
			timer.OnChildStart(200_000);
			timer.OnChildEnd(150_000); // completes [100,150]
			timer.OnChildAbandoned(200_000); // drops the open sibling without adding time to abandon

			timer.Duration.Should().Be(50);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void OnChildAbandoned_UnknownStart_IsIgnored()
		{
			var timer = new ChildDurationTimer();

			timer.OnChildStart(100_000);
			timer.OnChildAbandoned(999_000);

			timer.ActiveChildren.Should().Be(1);
			timer.Duration.Should().Be(0);

			timer.OnChildEnd(200_000);
			timer.Duration.Should().Be(100);
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

			timer.OnChildStart(400_000);
			timer.OnChildEnd(500_000);
			timer.Duration.Should().Be(200);
			timer.ActiveChildren.Should().Be(0);
		}

		[Fact]
		public void EndBeforeStartTimestamp_ClampsToZeroContribution()
		{
			var timer = new ChildDurationTimer();

			timer.OnChildStart(200_000);
			timer.OnChildEnd(100_000);

			timer.Duration.Should().Be(0);
			timer.ActiveChildren.Should().Be(0);
		}
	}
}
