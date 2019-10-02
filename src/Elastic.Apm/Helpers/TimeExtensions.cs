using System;
using System.Globalization;
using System.Text;

namespace Elastic.Apm.Helpers
{
	internal static class TimeExtensions
	{
		internal static string FormatForLog(this DateTime dateTime, bool includeKind = true) =>
			dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + (includeKind ? $" {dateTime.Kind.FormatForLog()}" : "");

		internal static string FormatForLog(this DateTimeKind dateTimeKind)
		{
			switch (dateTimeKind)
			{
				case DateTimeKind.Utc: return "UTC";
				case DateTimeKind.Local: return "Local";
				case DateTimeKind.Unspecified: return "Unspecified";
				default: return $"UNRECOGNIZED {dateTimeKind} ({(int)dateTimeKind} as int)";
			}
		}

		internal static string FormatForLog(this DateTimeOffset dateTimeOffset) =>
			dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture);

		/// <summary>
		/// Converts time duration to "9d 8h 7m 6s 5ms" string representation
		/// </summary>
		internal static string ToHms(this TimeSpan timeSpan)
		{
			if (timeSpan == TimeSpan.Zero) return "0";

			var strBuilder = new StringBuilder();

			if (timeSpan < TimeSpan.Zero) strBuilder.Append("-");

			var hasParts = false;

			AppendIfNotZero(timeSpan.Days, "d");
			AppendIfNotZero(timeSpan.Hours, "h");
			AppendIfNotZero(timeSpan.Minutes, "m");
			AppendIfNotZero(timeSpan.Seconds, "s");
			AppendIfNotZero(timeSpan.Milliseconds, "ms");

			// 1 tick is 100 nanoseconds
			var ticks = Math.Abs(timeSpan.Ticks);
			var nanoseconds = ticks % 10 * 100;
			var microseconds = (ticks - nanoseconds / 100) % 10_000 / 10;
			Assertion.IfEnabled?.That(0 <= microseconds && microseconds <= 999
				, "Because every microsecond above 999 should overflow into milliseconds."
				+ $" {nameof(microseconds)}: {microseconds}."
				+ $" {nameof(timeSpan)}: {timeSpan}.");
			AppendIfNotZero((int)microseconds, "us");
			Assertion.IfEnabled?.That(0 <= nanoseconds && nanoseconds <= 900
				, "Because the resolution is 1 tick which is 100 nanoseconds."
				+ $" {nameof(nanoseconds)}: {nanoseconds}."
				+ $" {nameof(timeSpan)}: {timeSpan}.");
			AppendIfNotZero((int)nanoseconds, "ns");

			return strBuilder.ToString();

			void AppendIfNotZero(int count, string unitsSuffix)
			{
				if (count == 0) return;

				if (hasParts) strBuilder.Append(" ");
				else hasParts = true;

				strBuilder.Append(Math.Abs(count)).Append(unitsSuffix);
			}
		}

		private const int NumberOfTicksPerSecond = 10_000_000;

		/// <summary>
		/// Converts time duration to "9d 8h 7m 6s" (seconds resolution) string representation.
		/// If time duration has non-integer number of seconds the fractional part is truncated.
		/// If time duration is [0, 1s) range it is converted to "<1s".
		/// If time duration is (-1s, 0] range it is converted to ">-1s".
		/// </summary>
		internal static string ToHmsInSeconds(this TimeSpan timeSpan)
		{
			if (timeSpan == TimeSpan.Zero) return "0s";
			var truncated = TruncateToSeconds(timeSpan);
			if (truncated != TimeSpan.Zero) return truncated.ToHms();

			return timeSpan > TimeSpan.Zero ? "<1s" : ">-1s";
		}

		internal static TimeSpan TruncateToSeconds(this TimeSpan timeSpan) =>
			TimeSpan.FromTicks(Math.Sign(timeSpan.Ticks) * (Math.Abs(timeSpan.Ticks) / NumberOfTicksPerSecond) * NumberOfTicksPerSecond);
	}
}
