using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class AssertionTests
	{
		[Fact]
		public void PassedAssertion() => Assertion.IfEnabled?.That(true, "Dummy message");

		[Fact]
		public void FailedAssertion() =>
			AsAction(() => Assertion.IfEnabled?.That(false, "Dummy message"))
				.Should()
				.ThrowExactly<AssertionFailedException>()
				.WithMessage("Dummy message");

		[Fact]
		public void WhenDisabledConditionAndMessageAreNotEvaluated()
		{
			var expensiveCallsCount = 0;

			bool ExpensiveCall(bool result)
			{
				++expensiveCallsCount;
				return result;
			}

			var isEnabledValueToRestore = Assertion.IsEnabled;
			Assertion.IsEnabled = false;
			try
			{
				Assertion.IfEnabled?.That(ExpensiveCall(true), $"ExpensiveCall(true): {ExpensiveCall(true)}");
				expensiveCallsCount.Should().Be(0);
				Assertion.IfEnabled?.That(ExpensiveCall(false), $"ExpensiveCall(true): {ExpensiveCall(false)}");
				expensiveCallsCount.Should().Be(0);
			}
			finally
			{
				Assertion.IsEnabled = isEnabledValueToRestore;
			}
		}
	}
}
