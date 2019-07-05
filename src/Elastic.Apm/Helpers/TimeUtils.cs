using System;

namespace Elastic.Apm.Helpers
{
	internal static class TimeUtils
	{
		/// <summary>
		/// DateTime.UnixEpoch Field does not exist in .NET Standard 2.0
		/// https://docs.microsoft.com/en-us/dotnet/api/system.datetime.unixepoch
		/// </summary>
		internal static readonly DateTime UnixEpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		internal static long TimestampNow() => ToTimestamp(DateTime.UtcNow);

		/// <summary>
		/// UTC based and formatted as microseconds since Unix epoch.
		/// </summary>
		/// <param name="dateTimeToConvert">DateTime instance to convert to timestamp - its <see cref="DateTime.Kind"/> should be <see cref="DateTimeKind.Utc"/></param>
		/// <returns>UTC based and formatted as microseconds since Unix epoch</returns>
		internal static long ToTimestamp(DateTime dateTimeToConvert)
		{
			if (dateTimeToConvert.Kind != DateTimeKind.Utc)
				throw new ArgumentException($"{nameof(dateTimeToConvert)}'s Kind should be UTC but instead its Kind is {dateTimeToConvert.Kind}" +
					$". {nameof(dateTimeToConvert)}'s value: {dateTimeToConvert.FormatForLog()}", nameof(dateTimeToConvert));

			return RoundTimeValue((dateTimeToConvert - UnixEpochDateTime).TotalMilliseconds * 1000);
		}

		internal static DateTime ToDateTime(long timestamp) => UnixEpochDateTime + TimeSpan.FromTicks(timestamp * 10);

		internal static string FormatTimestampForLog(long timestamp) => ToDateTime(timestamp).FormatForLog();

		/// <summary>
		/// Duration between timestamps in ms with 3 decimal points
		/// </summary>
		/// <returns>Duration between timestamps in ms with 3 decimal points</returns>
		internal static double DurationBetweenTimestamps(long startTimestamp, long endTimestamp) => (endTimestamp-startTimestamp)/1000.0;

		internal static DateTime ToEndDateTime(long startTimestamp, double duration) =>
			ToDateTime(RoundTimeValue(startTimestamp + duration * 1000));

		internal static TimeSpan TimeSpanFromFractionalMilliseconds(double fractionalMilliseconds) =>
			TimeSpan.FromTicks(RoundTimeValue(fractionalMilliseconds * TimeSpan.TicksPerMillisecond));

		internal static long RoundTimeValue(double value) => (long)Math.Round(value, MidpointRounding.AwayFromZero);
	}
}
