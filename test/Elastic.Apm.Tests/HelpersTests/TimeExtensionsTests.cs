// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using FluentAssertions;
using FluentAssertions.Extensions;
using Xunit;

namespace Elastic.Apm.Tests.HelpersTests
{
	public class TimeExtensionsTests
	{
		private static readonly ValueTuple<TimeSpan, string>[] ToHmsStringVariantsInternal =
		{
			(TimeSpan.Zero, "0"), (1.Nanoseconds(), "0"), (49.Nanoseconds(), "0"), (51.Nanoseconds(), "100ns"), (99.Nanoseconds(), "100ns"),
			(100.Nanoseconds(), "100ns"), (150.Nanoseconds(), "200ns"), (199.Nanoseconds(), "200ns"), (200.Nanoseconds(), "200ns"),
			(1200.Nanoseconds(), "1us 200ns"), (1.Microseconds(), "1us"), (1.Milliseconds(), "1ms"), (2.Seconds(), "2s"), (3.Minutes(), "3m"),
			(4.Hours(), "4h"), (5.Days(), "5d"), (678.Days(), "678d"),
			(9.Days() + 8.Hours() + 7.Minutes() + 6.Seconds() + 5.Milliseconds(), "9d 8h 7m 6s 5ms"), (
				1200.Days() + 25.Hours() + 59.Minutes() + 52.Seconds() + 9876.Milliseconds() + 2345.Microseconds() + 6789.Nanoseconds()
				, "1201d 2h 1s 878ms 351us 800ns")
		};

		public static IEnumerable<object[]> ToHmsStringVariants => GenToHmsStringVariants();

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

		private static IEnumerable<object[]> GenToHmsStringVariants()
		{
			var variantIndex = 0;
			foreach (var (timeSpan, expectedHmsString) in AddNegativeVariants(ToHmsStringVariantsInternal))
				yield return new object[] { variantIndex++, timeSpan, expectedHmsString };

			IEnumerable<ValueTuple<TimeSpan, string>> AddNegativeVariants(IEnumerable<ValueTuple<TimeSpan, string>> source)
			{
				foreach (var (timeSpan, expectedHmsString) in source)
				{
					yield return (timeSpan, expectedHmsString);

					if (timeSpan != TimeSpan.Zero && !expectedHmsString.StartsWith("-") && timeSpan.Ticks % 100 == 0)
						yield return (-timeSpan, "-" + expectedHmsString);
				}
			}
		}

		[Theory]
		[MemberData(nameof(ToHmsStringVariants))]
		public void ToHmsString_tests(int variantIndex, TimeSpan timeSpan, string expectedHmsString) =>
			timeSpan.ToHms().Should().Be(expectedHmsString, $"variantIndex: {variantIndex}");

		public static IEnumerable<object[]> GenTruncateToSecondsVariants()
		{
			TimeSpan[] deltas =
			{
				TimeSpan.Zero, TimeSpan.FromTicks(1), 1.Nanoseconds(), 999.Nanoseconds(), 1.Microseconds(), 999.Microseconds(), 1.Milliseconds(),
				501.Milliseconds(), 789.Milliseconds(), 999.Milliseconds()
			};

			TimeSpan[] baseTimeSpans =
			{
				TimeSpan.Zero, 1.Seconds(), 59.Seconds(), 1.Minutes() + 7.Seconds(), 29.Minutes() + 23.Seconds(), 53.Minutes(), 1.Hours(),
				13.Hours() + 29.Minutes() + 23.Seconds(), 22.Hours() + 1.Minutes() + 7.Seconds()
			};

			IEnumerable<ValueTuple<TimeSpan, TimeSpan>> GenBasePlusDeltas()
			{
				foreach (var baseTimeSpan in baseTimeSpans)
				{
					foreach (var delta in deltas)
						yield return (baseTimeSpan + delta, baseTimeSpan);
				}
			}

			IEnumerable<ValueTuple<TimeSpan, TimeSpan>> AddNegativeVariants(IEnumerable<ValueTuple<TimeSpan, TimeSpan>> source)
			{
				foreach (var (timeSpan, expectedTruncatedTimeSpan) in source)
				{
					yield return (timeSpan, expectedTruncatedTimeSpan);

					if (timeSpan != TimeSpan.Zero) yield return (-timeSpan, -expectedTruncatedTimeSpan);
				}
			}

			var allVariants = AddNegativeVariants(GenBasePlusDeltas());

			var variantIndex = 0;
			foreach (var (timeSpan, expectedHmsString) in allVariants)
				yield return new object[] { variantIndex++, timeSpan, expectedHmsString };

			variantIndex.Should().BeGreaterThan(baseTimeSpans.Length * deltas.Length);
		}

		[Theory]
		[MemberData(nameof(GenTruncateToSecondsVariants))]
		public void TruncateToSeconds_tests(int variantIndex, TimeSpan timeSpan, TimeSpan expectedTruncatedTimeSpan) =>
			timeSpan.TruncateToSeconds()
				.Should()
				.Be(expectedTruncatedTimeSpan, $"variantIndex: {variantIndex}, timeSpan: {timeSpan.ToHms()}"
					+ $", expectedRoundedTimeSpan: {expectedTruncatedTimeSpan.ToHms()}");

		public static IEnumerable<object[]> GenToHmsInSecondsVariants()
		{
			TimeSpan[] deltas =
			{
				TimeSpan.Zero, TimeSpan.FromTicks(1), 1.Nanoseconds(), 999.Nanoseconds(), 1.Microseconds(), 999.Microseconds(), 1.Milliseconds(),
				501.Milliseconds(), 789.Milliseconds(), 999.Milliseconds()
			};

			ValueTuple<TimeSpan, string>[] baseVariants =
			{
				(TimeSpan.Zero, "0s"), (1.Seconds(), "1s"), (59.Seconds(), "59s"), (1.Minutes() + 7.Seconds(), "1m 7s"),
				(29.Minutes() + 23.Seconds(), "29m 23s"), (53.Minutes(), "53m"), (1.Hours() + 29.Minutes() + 23.Seconds(), "1h 29m 23s"),
				(13.Hours() + 59.Seconds(), "13h 59s"), (22.Hours() + 1.Minutes() + 7.Seconds(), "22h 1m 7s")
			};

			IEnumerable<ValueTuple<TimeSpan, string>> GenBasePlusDeltas()
			{
				foreach (var (baseTimeSpan, hmsInSeconds) in baseVariants)
				{
					foreach (var delta in deltas)
					{
						if (baseTimeSpan == TimeSpan.Zero)
						{
							if (delta == TimeSpan.Zero)
								yield return (baseTimeSpan + delta, hmsInSeconds);
							else
								yield return (delta, "<1s");
						}
						else
							yield return (baseTimeSpan + delta, hmsInSeconds);
					}
				}
			}

			IEnumerable<ValueTuple<TimeSpan, string>> AddNegativeVariants(IEnumerable<ValueTuple<TimeSpan, string>> source)
			{
				foreach (var (timeSpan, hmsInSeconds) in source)
				{
					yield return (timeSpan, hmsInSeconds);

					if (timeSpan == TimeSpan.Zero) continue;

					if (timeSpan >= 1.Seconds())
						yield return (-timeSpan, "-" + hmsInSeconds);
					else
						yield return (-timeSpan, ">-1s");
				}
			}

			var allVariants = AddNegativeVariants(GenBasePlusDeltas());

			var variantIndex = 0;
			foreach (var (timeSpan, expectedHmsString) in allVariants)
				yield return new object[] { variantIndex++, timeSpan, expectedHmsString };

			variantIndex.Should().BeGreaterThan(baseVariants.Length * deltas.Length);
		}

		[Theory]
		[MemberData(nameof(GenToHmsInSecondsVariants))]
		public void ToHmsInSeconds_tests(int variantIndex, TimeSpan timeSpan, string expectedToHmsInSeconds) =>
			timeSpan.ToHmsInSeconds()
				.Should()
				.Be(expectedToHmsInSeconds, $"variantIndex: {variantIndex}, timeSpan: {timeSpan.ToHms()}"
					+ $", expectedToHmsInSeconds: {expectedToHmsInSeconds}");
	}
}
