using System;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;
using static Elastic.Apm.Tests.TestHelpers.FluentAssertionsUtils;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class TimeUtilsTests
	{
		private static readonly DateTime UnixEpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static TheoryData TimestampAndDateTimeVariantsToTest => new TheoryData<long, DateTime>
		{
			{ 0, UnixEpochDateTime },
			{ 1, UnixEpochDateTime + TimeUtils.TimeSpanFromFractionalMilliseconds(0.001) },
			{ 10, UnixEpochDateTime + TimeUtils.TimeSpanFromFractionalMilliseconds(0.01) },
			{ 100, UnixEpochDateTime + TimeUtils.TimeSpanFromFractionalMilliseconds(0.1) },
			{ 1_000, new DateTime(1970, 1, 1, 0, 0, 0, 1, DateTimeKind.Utc) },
			{ 2_000_000 + 1_000, new DateTime(1970, 1, 1, 0, 0, 2, 1, DateTimeKind.Utc) },
			{ 3 * 60 * 1_000_000 + 2_000_000 + 1_000, new DateTime(1970, 1, 1, 0, 3, 2, 1, DateTimeKind.Utc) },
			{ 4 * 60 * 60 * 1_000_000L, new DateTime(1970, 1, 1, 4, 0, 0, DateTimeKind.Utc) },
			{ 5 * 24 * 60 * 60 * 1_000_000L, new DateTime(1970, 1, 6, 0, 0, 0, DateTimeKind.Utc) },
		};

		[Fact]
		public void TimeUtilsUnixEpochDateTimeEqualsDateTimeUnixEpoch() => TimeUtils.UnixEpochDateTime.Should().Be(DateTime.UnixEpoch);

		[Theory]
		[InlineData(0)]
		[InlineData(1.0)]
		[InlineData(1.0 / TimeSpan.TicksPerMillisecond)]
		[InlineData(123.4567)]
		[InlineData(-1.0 * 0.0)] // signed negative zero
		[InlineData(-1.0)]
		[InlineData(-1.0 / TimeSpan.TicksPerMillisecond)]
		[InlineData(-123.4567)]
		public void TimeSpanFromFractionalMillisecondsExactTests(double fractionalMilliseconds) =>
			ShouldBeWithTolerance(TimeUtils.TimeSpanFromFractionalMilliseconds(fractionalMilliseconds).TotalMilliseconds, fractionalMilliseconds);

		[Theory]
		[InlineData(0.00004, 0.0000)]
		[InlineData(0.00005, 0.0001)]
		[InlineData(0.00014, 0.0001)]
		[InlineData(0.0001500001, 0.0002)]
		[InlineData(123.45674, 123.4567)]
		[InlineData(123.45675, 123.4568)]
		[InlineData(-0.00004, -0.0000)]
		[InlineData(-0.00005, -0.0001)]
		[InlineData(-0.00014, -0.0001)]
		[InlineData(-0.0001500001, -0.0002)]
		[InlineData(-123.45674, -123.4567)]
		[InlineData(-123.45675, -123.4568)]
		public void TimeSpanFromFractionalMillisecondsRoundedTests(double fractionalMilliseconds, double expectedRounded) =>
			ShouldBeWithTolerance(TimeUtils.TimeSpanFromFractionalMilliseconds(fractionalMilliseconds).TotalMilliseconds, expectedRounded);

		[Fact]
		public void TimestampNowShouldUseCurrentTime()
		{
			var beforeNowAsTimestamp = TimeUtils.ToTimestamp(DateTime.UtcNow);
			var nowAsTimestamp = TimeUtils.TimestampNow();
			var afterNowAsTimestamp = TimeUtils.ToTimestamp(DateTime.UtcNow);
			nowAsTimestamp.Should().BeInRange(beforeNowAsTimestamp, afterNowAsTimestamp);
		}

		[Theory]
		[MemberData(nameof(TimestampAndDateTimeVariantsToTest))]
		public void TimestampToDateTimeTests(long timestamp, DateTime dateTime)
		{
			TimeUtils.ToDateTime(timestamp).Should().Be(dateTime);
			TimeUtils.ToDateTime(timestamp).Kind.Should().Be(DateTimeKind.Utc);
		}

		[Theory]
		[MemberData(nameof(TimestampAndDateTimeVariantsToTest))]
		public void DateTimeToTimestampTests(long timestamp, DateTime dateTime) =>
			TimeUtils.ToTimestamp(dateTime).Should().Be(timestamp);

		[Fact]
		public void DateTimeToTimestampThrowsIfNotUtc()
		{
			var localNow = DateTime.Now;
			localNow.Kind.Should().Be(DateTimeKind.Local);
			AsAction(() => TimeUtils.ToTimestamp(localNow)).Should().ThrowExactly<ArgumentException>().WithMessage($"*{localNow.FormatForLog()}*");
		}

		[Theory]
		[InlineData(0, 0, 0.0)]
		[InlineData(1561954166179000, 1561954166195000, 16)]
		[InlineData(1561954166195000, 1561954166179000, -16)]
		[InlineData(1561954166179856, 1561954166195481, 15.625)]
		[InlineData(1561954166195481, 1561954166179856, -15.625)]
		public void DurationBetweenTimestampsTests(long startTimestamp, long endTimestamp, double expectedDuration) =>
			TimeUtils.DurationBetweenTimestamps(startTimestamp, endTimestamp).Should().Be(expectedDuration);

		private static void ShouldBeWithTolerance(double actual, double expected)
		{
			const double tolerance = 0.000001;
			actual.Should().BeInRange(expected - tolerance, expected + tolerance);
		}
	}
}
