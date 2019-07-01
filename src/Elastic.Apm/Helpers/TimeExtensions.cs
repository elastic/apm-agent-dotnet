using System;
using System.Globalization;

namespace Elastic.Apm.Helpers
{
	public static class TimeExtensions
	{
		public static string FormatForLog(this DateTimeOffset dateTimeOffset) =>
			dateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss.ffffff zzz", CultureInfo.InvariantCulture);
	}
}
