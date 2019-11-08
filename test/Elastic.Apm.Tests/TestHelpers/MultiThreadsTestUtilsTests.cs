using System;
using Elastic.Apm.Tests.Extensions;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class MultiThreadsTestUtilsTests
	{
		[Fact]
		public void thread_action_returns_int()
		{
			var results = MultiThreadsTestUtils.TestOnThreads(threadIndex => threadIndex * threadIndex);
			results.ForEachIndexed((threadResult, threadIndex) => threadResult.Should().Be(threadIndex * threadIndex));
		}

		[Fact]
		public void thread_action_returns_string()
		{
			var results = MultiThreadsTestUtils.TestOnThreads(threadIndex => (threadIndex * threadIndex).ToString());
			results.ForEachIndexed((threadResult, threadIndex) => threadResult.Should().Be((threadIndex * threadIndex).ToString()));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void thread_action_throws(int throwingThreadIndex)
		{
			var exceptionWasThrown = false;
			try
			{
				MultiThreadsTestUtils.TestOnThreads<object>(threadIndex =>
				{
					if (throwingThreadIndex == threadIndex) throw new DummyTestException(threadIndex.ToString());

					return null;
				});
			}
			catch (AggregateException ex)
			{
				exceptionWasThrown = true;
				ex.InnerExceptions.Should().ContainSingle();
				// ReSharper disable once PossibleNullReferenceException
				ex.InnerException.GetType().Should().Be(typeof(DummyTestException));
				ex.InnerException.Message.Should().Be(throwingThreadIndex.ToString());
			}
			exceptionWasThrown.Should().BeTrue();
		}
	}
}
