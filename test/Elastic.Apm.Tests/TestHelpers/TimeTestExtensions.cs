// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;

namespace Elastic.Apm.Tests.TestHelpers
{
	internal static class TimeTestExtensions
	{
		internal static TimeSpan Nanosecond(this int amount) => TimeSpan.FromTicks((long)Math.Round(amount * 0.01));

		internal static TimeSpan Microsecond(this int amount) => TimeSpan.FromTicks(amount * 10);

		internal static TimeSpan Millisecond(this int amount) => TimeSpan.FromMilliseconds(amount);

		internal static TimeSpan Second(this int amount) => TimeSpan.FromSeconds(amount);

		internal static TimeSpan Minute(this int amount) => TimeSpan.FromMinutes(amount);

		internal static TimeSpan Hour(this int amount) => TimeSpan.FromHours(amount);

		internal static TimeSpan Day(this int amount) => TimeSpan.FromDays(amount);
	}
}
