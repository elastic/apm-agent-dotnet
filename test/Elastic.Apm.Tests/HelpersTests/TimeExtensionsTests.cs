using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class TimeExtensionsTests
	{
		[Theory]
		[InlineData(DateTimeKind.Utc, "UTC")]
		[InlineData(DateTimeKind.Local, "Local")]
		[InlineData(DateTimeKind.Unspecified, "Unspecified")]
		[InlineData((DateTimeKind)987, "UNRECOGNIZED 987 (987 as int)")]
		public void DateTimeKindFormatForLogTests(DateTimeKind dateTimeKind, string expectedFormattedForLog) =>
			dateTimeKind.FormatForLog().Should().Be(expectedFormattedForLog);

		public static IEnumerable<object[]> DateTimeFormattedForLogVariantsToTest()
		{
			yield return new object[]
			{
				new DateTime(1234, 5, 16, 17, 28, 39, DateTimeKind.Utc) +
				TimeUtils.TimeSpanFromFractionalMilliseconds(987.654),
				"1234-05-16 17:28:39.9876540 UTC"
			};

			yield return new object[]
			{
				new DateTime(1987, 12, 31, 4, 5, 6, DateTimeKind.Local) + TimeSpan.FromMilliseconds(123), "1987-12-31 04:05:06.1230000 Local"
			};

			yield return new object[]
			{
				new DateTime(2020, 2, 29, 23, 59, 58, DateTimeKind.Unspecified) + TimeSpan.FromMilliseconds(7),
				"2020-02-29 23:59:58.0070000 Unspecified"
			};
		}

		[Theory]
		[MemberData(nameof(DateTimeFormattedForLogVariantsToTest))]
		public void DateTimeFormatForLogTests(DateTime dateTime, string expectedFormattedForLog) =>
			dateTime.FormatForLog().Should().Be(expectedFormattedForLog);

		public static IEnumerable<object[]> DateTimeOffsetFormattedForLogVariantsToTest()
		{
			yield return new object[]
			{
				new DateTimeOffset(
					new DateTime(1234, 5, 16, 17, 28, 39, DateTimeKind.Utc) + TimeUtils.TimeSpanFromFractionalMilliseconds(987.654)),
				"1234-05-16 17:28:39.9876540 +00:00"
			};

			yield return new object[]
			{
				new DateTimeOffset(new DateTime(1987, 12, 31, 4, 5, 6, DateTimeKind.Utc) + TimeSpan.FromMilliseconds(123)),
				"1987-12-31 04:05:06.1230000 +00:00"
			};

			yield return new object[]
			{
				new DateTimeOffset(new DateTime(2020, 2, 29, 23, 59, 58, DateTimeKind.Utc) + TimeSpan.FromMilliseconds(7)),
				"2020-02-29 23:59:58.0070000 +00:00"
			};
		}

		[Theory]
		[MemberData(nameof(DateTimeOffsetFormattedForLogVariantsToTest))]
		public void DateTimeOffsetFormatForLogTests(DateTimeOffset dateTimeOffset, string expectedFormattedForLog) =>
			dateTimeOffset.FormatForLog().Should().Be(expectedFormattedForLog);
	}
}
