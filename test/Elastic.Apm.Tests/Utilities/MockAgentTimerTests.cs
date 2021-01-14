// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Helpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using static Elastic.Apm.Tests.Utilities.FluentAssertionsUtils;

namespace Elastic.Apm.Tests.Utilities
{
	public class MockAgentTimerTests
	{
		[Fact]
		internal void Now_without_FastForward_returns_the_same_instant()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());
			var i1 = agentTimer.Now;
			var i1B = agentTimer.Now;
			i1.Equals(i1B).Should().BeTrue();
			i1.Equals((object)i1B).Should().BeTrue();
			(i1 == i1B).Should().BeTrue();
			(i1 != i1B).Should().BeFalse();

			var diffBetweenI21 = 0.Days() + 9.Hours() + 8.Minutes() + 7.Seconds() + 6.Milliseconds();
			agentTimer.FastForward(diffBetweenI21);
			var i2 = agentTimer.Now;
			var i2B = agentTimer.Now;
			i1.Equals(i2).Should().BeFalse();
			i1.Equals((object)i2).Should().BeFalse();
			(i2 == i2B).Should().BeTrue();

			(i1 == i2).Should().BeFalse();
			(i1 != i2).Should().BeTrue();

			(i1 + diffBetweenI21).Should().Be(i2);
			var i2C = i1;
			i2C += diffBetweenI21;
			i2C.Should().Be(i2);
		}

		[Fact]
		public void Delay_simple_test()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());

			var delayTask = agentTimer.Delay(agentTimer.Now + 2.Days());
			delayTask.IsCompleted.Should().BeFalse();
			agentTimer.FastForward(1.Days());
			delayTask.IsCompleted.Should().BeFalse();
			agentTimer.FastForward(1.Days());
			delayTask.IsCompleted.Should().BeTrue();
		}

		[Fact]
		public void Delay_fast_forward_past_trigger_time()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());

			var delayTask = agentTimer.Delay(agentTimer.Now + 2.Minutes());
			delayTask.IsCompleted.Should().BeFalse();
			agentTimer.FastForward(3.Minutes());
			delayTask.IsCompleted.Should().BeTrue();
		}

		[Fact]
		public void calling_FastForward_while_one_already_in_progress_throws()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());

			var reachedBeforeInnerFastForward = false;
			var reachedAfterInnerFastForward = false;
			agentTimer.Delay(agentTimer.Now + 1.Hours())
				.AttachSynchronousContinuation(() =>
				{
					AsAction(() =>
						{
							reachedBeforeInnerFastForward = true;
							agentTimer.FastForward(1.Minutes());
							reachedAfterInnerFastForward = true;
						})
						.Should()
						.ThrowExactly<InvalidOperationException>()
						.WithMessage($"*{nameof(MockAgentTimer.FastForward)}*");
				});

			agentTimer.FastForward(2.Hours());

			reachedBeforeInnerFastForward.Should().BeTrue();
			reachedAfterInnerFastForward.Should().BeFalse();
		}

		[Fact]
		public void Cancel_Delay()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());
			using (var cancellationTokenSource = new CancellationTokenSource())
			{
				var continuationCalled = false;

				agentTimer.Delay(agentTimer.Now + 30.Minutes(), cancellationTokenSource.Token)
					.AttachSynchronousContinuation(task =>
					{
						continuationCalled.Should().BeFalse();
						continuationCalled = true;
						task.IsCanceled.Should().BeTrue();
					});

				agentTimer.FastForward(20.Minutes());

				cancellationTokenSource.Cancel();
				continuationCalled.Should().BeTrue();

				agentTimer.FastForward(20.Minutes());
			}
		}

		[Fact]
		public void Cancel_already_triggered_Delay()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());
			using (var cancellationTokenSource = new CancellationTokenSource())
			{
				var continuationCalled = false;

				agentTimer.Delay(agentTimer.Now + 30.Minutes(), cancellationTokenSource.Token)
					.AttachSynchronousContinuation(task =>
					{
						continuationCalled.Should().BeFalse();
						continuationCalled = true;
						task.IsCanceled.Should().BeFalse();
					});

				agentTimer.FastForward(30.Minutes());
				continuationCalled.Should().BeTrue();

				cancellationTokenSource.Cancel();
			}
		}

		[Fact]
		public void Two_Delays_longer_first()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());

			var t1 = 7.Seconds();
			var t2 = 6.Seconds();
			var startInstant = agentTimer.Now;
			var delayTaskA = agentTimer.Delay(agentTimer.Now + t1 + t2);
			var delayTaskB = agentTimer.Delay(agentTimer.Now + t1);
			delayTaskA.IsCompleted.Should().BeFalse();
			delayTaskB.IsCompleted.Should().BeFalse();
			agentTimer.FastForward(t1);
			agentTimer.Now.Should().Be(startInstant + t1);
			delayTaskA.IsCompleted.Should().BeFalse();
			delayTaskB.IsCompleted.Should().BeTrue();
			agentTimer.FastForward(t2);
			agentTimer.Now.Should().Be(startInstant + t1 + t2);
			delayTaskA.IsCompleted.Should().BeTrue();
		}

		[Fact]
		public void Add_Delay_in_continuation()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());

			var t1 = 7.Minutes();
			var t2 = 7.Seconds();
			var t3 = 7.Days();

			var invocationCounter = 0;
			var startInstant = agentTimer.Now;

			agentTimer.Delay(agentTimer.Now + t1)
				.AttachSynchronousContinuation(() =>
				{
					(++invocationCounter).Should().Be(1);
					agentTimer.Now.Should().Be(startInstant + t1);

					agentTimer.Delay(agentTimer.Now + t2)
						.AttachSynchronousContinuation(() =>
						{
							(++invocationCounter).Should().Be(2);
							agentTimer.Now.Should().Be(startInstant + t1 + t2);
						});
				});

			agentTimer.Delay(agentTimer.Now + t1 + t2 + t3)
				.AttachSynchronousContinuation(() =>
				{
					(++invocationCounter).Should().Be(3);
					agentTimer.Now.Should().Be(startInstant + t1 + t2 + t3);
				});

			agentTimer.FastForward(t1 + t2 + t3);
		}

		[Fact]
		public void Delay_with_relativeToInstant_in_the_past()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());

			var t1 = 7.Seconds();
			var t2 = 11.Seconds();
			var timeToWait = t1 + t2;

			// We take time instant at the start of a "atomic" calculation
			var now = agentTimer.Now;
			// Let's assume there's a long calculation that takes some time
			agentTimer.FastForward(t1);
			// and the conclusion of the calculation that we need to delay for timeToWait
			// (relative to `now` time instant)
			var delayTask = agentTimer.Delay(now + timeToWait);
			delayTask.IsCompleted.Should().BeFalse();

			agentTimer.FastForward(t2);

			delayTask.IsCompleted.Should().BeTrue();
		}

		[Fact]
		public void Delay_with_target_time_is_already_in_the_past()
		{
			var agentTimer = new MockAgentTimer(DbgUtils.CurrentMethodName());

			var timeToWait = 3.Seconds();

			// We take time instant at the start of a "atomic" calculation
			var now = agentTimer.Now;
			// Let's assume there's a long calculation that takes some time
			agentTimer.FastForward(timeToWait + 5.Seconds());
			// and the conclusion of the calculation that we need to delay for timeToWait
			// (relative to `now` time instant)
			var delayTask = agentTimer.Delay(now + timeToWait);
			delayTask.IsCompleted.Should().BeTrue();
		}
	}

	internal static class MockAgentTimerTestsExtensions
	{
		internal static Task AttachSynchronousContinuation(this Task thisObj, Action action) =>
			thisObj.ContinueWith(_ => action(), TaskContinuationOptions.ExecuteSynchronously);

		internal static Task AttachSynchronousContinuation(this Task thisObj, Action<Task> action) =>
			thisObj.ContinueWith(action, TaskContinuationOptions.ExecuteSynchronously);
	}
}
