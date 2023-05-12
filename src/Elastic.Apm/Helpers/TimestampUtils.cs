// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Globalization;

namespace Elastic.Apm.Helpers
{
	internal static class TimestampUtils
	{
		/// <summary>
		/// DateTimeOffset.UnixEpoch Field does not exist in .NET Standard 2.0
		/// https://docs.microsoft.com/en-us/dotnet/api/system.datetime.unixepoch
		/// </summary>
		internal static readonly DateTimeOffset UnixEpochDateTime = new (1970, 1, 1, 0, 0, 0, 0, new GregorianCalendar(), TimeSpan.Zero);

		internal static long TimestampNow() => ToTimestamp(DateTimeOffset.UtcNow);

		internal static long ToTimestamp(DateTimeOffset dateTime) =>
			DurationUtils.Round((dateTime - UnixEpochDateTime).Ticks / 10.0);

		/// <summary>
		/// UTC based and formatted as microseconds since Unix epoch.
		/// </summary>
		/// <param name="dateTime">
		/// DateTime instance to convert to timestamp - its <see cref="DateTime.Kind" /> should be
		/// <see cref="DateTimeKind.Utc" />
		/// </param>
		/// <returns>UTC based and formatted as microseconds since Unix epoch</returns>
		internal static long ToTimestamp(DateTime dateTime)
		{
			if (dateTime.Kind != DateTimeKind.Utc)
			{
				throw new ArgumentException($"{nameof(dateTime)}'s Kind should be UTC but instead its Kind is {dateTime.Kind}" +
					$". {nameof(dateTime)}'s value: {dateTime.FormatForLog()}", nameof(dateTime));
			}
			var diff = dateTime - UnixEpochDateTime;
			return DurationUtils.Round(diff.Ticks / 10);
		}

		internal static DateTimeOffset ToDateTimeOffset(long timestamp) => UnixEpochDateTime + TimeSpan.FromTicks(timestamp * 10);

		internal static string FormatTimestampForLog(long timestamp) => ToDateTimeOffset(timestamp).FormatForLog();

		/// <summary>
		/// Duration between timestamps in ms with 3 decimal points
		/// </summary>
		/// <returns>Duration between timestamps in ms with 3 decimal points</returns>
		internal static double DurationBetweenTimestamps(long startTimestamp, long endTimestamp) => (endTimestamp - startTimestamp) / 1000.0;
	}

	internal static class DurationUtils
	{
		internal static TimeSpan TimeSpanFromFractionalMilliseconds(double fractionalMilliseconds) =>
			TimeSpan.FromTicks(Round(fractionalMilliseconds * TimeSpan.TicksPerMillisecond));

		internal static long Round(double duration) => (long)Math.Round(duration, MidpointRounding.AwayFromZero);
	}
}
