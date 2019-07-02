using System;
using System.Globalization;

namespace Elastic.Apm.Helpers
{
	public static class TimeExtensions
	{
		public static string FormatForLog(this DateTime dateTime, bool includeKind = true) =>
			dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture) + (includeKind ? $" {dateTime.Kind.FormatForLog()}" : "");

		public static string FormatForLog(this DateTimeKind dateTimeKind)
		{
			switch (dateTimeKind)
			{
				case DateTimeKind.Utc: return "UTC";
				case DateTimeKind.Local: return "Local";
				case DateTimeKind.Unspecified: return "Unspecified";
				default: return $"UNRECOGNIZED {dateTimeKind} ({(int)dateTimeKind} as int)";
			}
		}

		public static string FormatForLog(this DateTimeOffset dateTimeOffset) =>
			dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.fffffff zzz", CultureInfo.InvariantCulture);
	}
}
