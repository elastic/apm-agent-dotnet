using System;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Elastic.Apm.Tests.TestHelpers
{
	public class TimeSpanExtensionsTests
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
			amount.Milliseconds().Should().Be(TimeSpan.FromMilliseconds(amount));
			amount.Millisecond().Should().Be(TimeSpan.FromMilliseconds(amount));
			amount.Seconds().Should().Be(TimeSpan.FromSeconds(amount));
			amount.Second().Should().Be(TimeSpan.FromSeconds(amount));
			amount.Minutes().Should().Be(TimeSpan.FromMinutes(amount));
			amount.Minute().Should().Be(TimeSpan.FromMinutes(amount));
			amount.Hours().Should().Be(TimeSpan.FromHours(amount));
			amount.Hour().Should().Be(TimeSpan.FromHours(amount));
			amount.Days().Should().Be(TimeSpan.FromDays(amount));
			amount.Day().Should().Be(TimeSpan.FromDays(amount));
		}
	}
}
