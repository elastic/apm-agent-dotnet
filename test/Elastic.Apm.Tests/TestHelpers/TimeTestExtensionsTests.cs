using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class TimeTestExtensionsTests
	{
		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(2)]
		[InlineData(12)]
		[InlineData(24)]
		[InlineData(60)]
		[InlineData(365)]
		[InlineData(-1)]
		[InlineData(-2)]
		[InlineData(-12)]
		[InlineData(-24)]
		[InlineData(-60)]
		[InlineData(-365)]
		public void test_unit_extensions(int amount)
		{
			amount.Nanosecond().Should().Be(amount.Nanoseconds());
			amount.Nanosecond().Should().Be(TimeSpan.FromTicks((long)Math.Round(amount / 100.0)));
			amount.Microsecond().Should().Be(amount.Microseconds());
			amount.Microsecond().Should().Be(TimeSpan.FromTicks(amount * 10));
			amount.Millisecond().Should().Be(amount.Milliseconds());
			amount.Millisecond().Should().Be(TimeSpan.FromMilliseconds(amount));
			amount.Second().Should().Be(amount.Seconds());
			amount.Second().Should().Be(TimeSpan.FromSeconds(amount));
			amount.Minute().Should().Be(amount.Minutes());
			amount.Minute().Should().Be(TimeSpan.FromMinutes(amount));
			amount.Hour().Should().Be(amount.Hours());
			amount.Hour().Should().Be(TimeSpan.FromHours(amount));
			amount.Day().Should().Be(amount.Days());
			amount.Day().Should().Be(TimeSpan.FromDays(amount));
		}
	}
}
