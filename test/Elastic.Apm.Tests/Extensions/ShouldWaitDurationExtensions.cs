using System.Threading;
using Elastic.Apm.Tests.Extensions;
using FluentAssertions;
using FluentAssertions.Numeric;

namespace Elastic.Apm.Tests
{
	public static class ShouldWaitDurationExtensions
	{
		public static AndConstraint<NumericAssertions<double>> BeGreaterOrEqualToMinimumSleepLength(this NullableNumericAssertions<double> duration) =>
			duration.NotBeNull().And.BeGreaterOrEqualTo(WaitHelpers.SleepLength);

		public static AndConstraint<NumericAssertions<double>> BeGreaterOrEqualToMinimumSleepLength(this NullableNumericAssertions<double> duration, int numberOfSleeps)
		{
			var expectedTransactionLength = numberOfSleeps * WaitHelpers.SleepLength;
			return duration.NotBeNull().And.BeGreaterOrEqualTo(expectedTransactionLength, $"we expected {numberOfSleeps} to influence the total duration");
		}
	}
}
