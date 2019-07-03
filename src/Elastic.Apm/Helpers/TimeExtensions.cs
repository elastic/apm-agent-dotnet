using System;
using System.Globalization;

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
	}
}
