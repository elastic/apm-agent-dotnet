using System;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class TimeSpanExtensions
	{
		internal static TimeSpan Millisecond(this int amount) => TimeSpan.FromMilliseconds(amount);

		internal static TimeSpan Second(this int amount) => TimeSpan.FromSeconds(amount);

		internal static TimeSpan Minute(this int amount) => TimeSpan.FromMinutes(amount);

		internal static TimeSpan Hour(this int amount) => TimeSpan.FromHours(amount);

		internal static TimeSpan Day(this int amount) => TimeSpan.FromDays(amount);
	}
}
