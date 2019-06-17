using System;

namespace Elastic.Apm.Tests.TestHelpers
{
	public static class TimeUtils
	{
		public static DateTimeOffset TimestampToDateTimeOffset(long timestampUtcMicroseconds) =>
			DateTimeOffset.FromUnixTimeMilliseconds(timestampUtcMicroseconds / 1000);

		public static DateTimeOffset TimestampDurationToEndDateTimeOffset(long timestampUtcMicroseconds, double durationMs) =>
			TimestampToDateTimeOffset((long)(timestampUtcMicroseconds + durationMs * 1000));
	}
}
