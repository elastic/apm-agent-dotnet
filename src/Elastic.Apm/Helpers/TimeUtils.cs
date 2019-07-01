using System;

namespace Elastic.Apm.Helpers
{
	internal static class TimeUtils
	{
		internal static long NowAsTimestamp() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

		internal static string FormatTimestampForLog(long timestamp) => DateTimeOffset.FromUnixTimeMilliseconds(timestamp/1000).FormatForLog();

		/// <summary>
		/// Duration between timestamps in ms with 3 decimal points
		/// </summary>
		/// <returns>Duration between timestamps in ms with 3 decimal points</returns>
		internal static double DurationBetweenTimestamps(long startTimestamp, long endTimestamp) => (endTimestamp-startTimestamp)/1000.0;

		internal static DateTimeOffset TimestampToDateTimeOffset(long timestampUtcMicroseconds) =>
			DateTimeOffset.FromUnixTimeMilliseconds(timestampUtcMicroseconds / 1000);

		internal static DateTimeOffset TimestampDurationToEndDateTimeOffset(long timestampUtcMicroseconds, double durationMs) =>
			TimestampToDateTimeOffset((long)(timestampUtcMicroseconds + durationMs * 1000));
	}
}
