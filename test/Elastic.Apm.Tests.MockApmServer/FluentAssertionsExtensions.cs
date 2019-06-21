using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;

namespace Elastic.Apm.Tests.MockApmServer
{
	internal static class FluentAssertionsExtensions
	{
		internal static void ShouldOccurBetween(this ITimedDto child, ITimedDto containingAncestor)
		{
			TimeUtils.TimestampToDateTimeOffset(child.Timestamp)
				.Should()
				.BeOnOrAfter(TimeUtils.TimestampToDateTimeOffset(containingAncestor.Timestamp));

			TimeUtils.TimestampDurationToEndDateTimeOffset(child.Timestamp, child.Duration)
				.Should()
				.BeOnOrBefore(TimeUtils.TimestampDurationToEndDateTimeOffset(containingAncestor.Timestamp, containingAncestor.Duration));
		}

		internal static void ShouldOccurBetween(this ITimestampedDto child, ITimedDto containingAncestor)
		{
			TimeUtils.TimestampToDateTimeOffset(child.Timestamp)
				.Should()
				.BeOnOrAfter(TimeUtils.TimestampToDateTimeOffset(containingAncestor.Timestamp))
				.And
				.BeOnOrBefore(TimeUtils.TimestampDurationToEndDateTimeOffset(containingAncestor.Timestamp, containingAncestor.Duration));
		}
	}
}
