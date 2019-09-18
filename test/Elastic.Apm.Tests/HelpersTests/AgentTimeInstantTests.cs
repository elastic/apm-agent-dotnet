using System;
using System.Collections.Generic;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Mocks;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;
using Xunit.Abstractions;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class AgentTimeInstantTests
	{
		private readonly IApmLogger _logger;

		public AgentTimeInstantTests(ITestOutputHelper testOutputHelper) => _logger = new XunitOutputLogger(testOutputHelper);

		public static IEnumerable<object[]> AgentTimeInstantSourceVariantsToTest()
		{
			var shortTimeSpans = new[] { 1.Milliseconds(), 23.Milliseconds(), 198.Milliseconds() };
			var agentTimers = new IAgentTimerForTesting[] { new AgentTimerForTesting(), new MockAgentTimer() };

			foreach (var agentTimer in agentTimers)
			foreach (var timeSpan in shortTimeSpans)
				yield return new[] { (object)agentTimer, timeSpan };

			var longTimeSpans = new[]
			{
				0.Days() + 9.Hours() + 8.Minutes() + 7.Seconds() + 6.Milliseconds(),
				1.Day() + 2.Hours() + 3.Minutes() + 4.Seconds() + 5.Milliseconds()
			};

			foreach (var timeSpan in longTimeSpans) yield return new[] { (object)new MockAgentTimer(), timeSpan };
		}

		public static IEnumerable<object[]> IncompatibleAgentTimeInstantSources()
		{
			var agentTimerSet1 = new IAgentTimerForTesting[] { new AgentTimerForTesting(), new MockAgentTimer() };
			var agentTimerSet2 = new IAgentTimerForTesting[] { new AgentTimerForTesting(), new MockAgentTimer() };
			foreach (var agentTimer1 in agentTimerSet1)
			foreach (var agentTimer2 in agentTimerSet2)
				yield return new object[] { agentTimer1, agentTimer2 };
		}

		[Theory]
		[MemberData(nameof(AgentTimeInstantSourceVariantsToTest))]
		internal void Instant_arithmetics(IAgentTimerForTesting agentTimer, TimeSpan timeToWaitToPass)
		{
			agentTimer.WaitForTimeToPass(timeToWaitToPass);
			var i1 = agentTimer.Now;
			var i1B = agentTimer.Now;
			i1.Equals(i1B).Should().Be(i1 == i1B);
			i1.Equals((object)i1B).Should().Be(i1 == i1B);
			(i1 != i1B).Should().Be(!(i1 == i1B));

			agentTimer.WaitForTimeToPass(timeToWaitToPass);
			var diffBetweenI21 = timeToWaitToPass;
			var i2 = agentTimer.Now;
			var i2B = agentTimer.Now;
			i2.Equals(i2B).Should().Be(i2 == i2B);
			i2.Equals((object)i2B).Should().Be(i2 == i2B);
			(i2 != i2B).Should().Be(!(i2 == i2B));

			i1.Equals(i2).Should().BeFalse();
			i1.Equals((object)i2).Should().BeFalse();
			(i1 == i2).Should().BeFalse();
			(i1 != i2).Should().BeTrue();

			(i1 < i2).Should().BeTrue();
			(i2 < i1).Should().BeFalse();
			(i1 < i1B).Should().Be(i1 != i1B);
			(i1B < i1).Should().BeFalse();

			(i1 > i2).Should().BeFalse();
			(i2 > i1).Should().BeTrue();
			(i2 > i2B).Should().BeFalse();
			(i1B > i1).Should().Be(i1B != i1);
			(i2B > i2).Should().Be(i2B != i2);

			(i1 <= i2).Should().BeTrue();
			(i2 <= i1).Should().BeFalse();
			(i1 <= i1B).Should().BeTrue();
			(i1B <= i1).Should().Be(i1B == i1);
			(i2 <= i2B).Should().BeTrue();
			(i2B <= i2).Should().Be(i2B == i2);

			(i1 >= i2).Should().BeFalse();
			(i2 >= i1).Should().BeTrue();
			(i1 >= i1B).Should().Be(i1 == i1B);
			(i1B >= i1).Should().BeTrue();
			(i2 >= i2B).Should().Be(i2 == i2B);
			(i2B >= i2).Should().BeTrue();

			(i1 + diffBetweenI21 <= i2).Should().BeTrue();
			// ReSharper disable once InvertIf
			if (i1 + diffBetweenI21 == i2)
			{
				var i2C = i1;
				i2C += diffBetweenI21;
				i2C.Should().Be(i2);

				(i2 - i1).Should().Be(diffBetweenI21);
				var i1C = i2;
				i1C -= diffBetweenI21;
				i1C.Should().Be(i1);
			}
		}

		[Theory]
		[MemberData(nameof(IncompatibleAgentTimeInstantSources))]
		internal void Instants_from_different_agentTimers_can_be_compared_for_equality(IAgentTimerForTesting agentTimer1,
			IAgentTimerForTesting agentTimer2
		)
		{
			var i1 = agentTimer1.Now;
			var i2 = agentTimer2.Now;
			_logger.Debug()?.Log("i1: {i1}", i1.ToStringDetailed());
			_logger.Debug()?.Log("i2: {i2}", i2.ToStringDetailed());
			(i1 == i2).Should().BeFalse();
			i1.Equals(i2).Should().BeFalse();
			i1.Equals((object)i2).Should().BeFalse();
		}

		[Theory]
		[MemberData(nameof(IncompatibleAgentTimeInstantSources))]
		internal void operations_on_Instants_from_different_agentTimers_throw(IAgentTimerForTesting agentTimer1, IAgentTimerForTesting agentTimer2)
		{
			var i1 = agentTimer1.Now;
			var i2 = agentTimer2.Now;

			AsAction(() => DummyNoopFunc(i2 - i1))
				.Should()
				.ThrowExactly<InvalidOperationException>()
				.WithMessage("*illegal to perform operation op_Subtraction *");

			AsAction(() => DummyNoopFunc(i2 > i1))
				.Should()
				.ThrowExactly<InvalidOperationException>()
				.WithMessage("*illegal to perform operation op_GreaterThan *");

			AsAction(() => DummyNoopFunc(i2 >= i1))
				.Should()
				.ThrowExactly<InvalidOperationException>()
				.WithMessage("*illegal to perform operation op_GreaterThanOrEqual *");

			AsAction(() => DummyNoopFunc(i2 < i1))
				.Should()
				.ThrowExactly<InvalidOperationException>()
				.WithMessage("*illegal to perform operation op_LessThan *");

			AsAction(() => DummyNoopFunc(i2 <= i1))
				.Should()
				.ThrowExactly<InvalidOperationException>()
				.WithMessage("*illegal to perform operation op_LessThanOrEqual *");

			// ReSharper disable once UnusedParameter.Local
			void DummyNoopFunc<T>(T _) { }
		}
	}
}
