// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Tests.Extensions;
using FluentAssertions;
using FluentAssertions.Numeric;

namespace Elastic.Apm.Tests
{
	public static class ShouldWaitDurationExtensions
	{
		public static AndConstraint<NumericAssertions<double>>
			BeGreaterOrEqualToMinimumSleepLength(this NullableNumericAssertions<double> duration) =>
			duration.NotBeNull().And.BeGreaterOrEqualTo(WaitHelpers.SleepLength);

		public static AndConstraint<NumericAssertions<double>> BeGreaterOrEqualToMinimumSleepLength(this NullableNumericAssertions<double> duration,
			int numberOfSleeps
		)
		{
			var expectedTransactionLength = numberOfSleeps * WaitHelpers.SleepLength;
			return duration.NotBeNull()
				.And.BeGreaterOrEqualTo(expectedTransactionLength, $"we expected {numberOfSleeps} to influence the total duration");
		}
	}
}
