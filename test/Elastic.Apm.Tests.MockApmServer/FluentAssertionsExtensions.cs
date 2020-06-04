// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Helpers;
using FluentAssertions;

namespace Elastic.Apm.Tests.MockApmServer
{
	internal static class FluentAssertionsExtensions
	{
		internal static void ShouldOccurBetween(this ITimestampedDto child, ITimedDto containingAncestor) =>
			TimeUtils.ToDateTime(child.Timestamp)
				.Should()
				.BeOnOrAfter(TimeUtils.ToDateTime(containingAncestor.Timestamp))
				.And
				.BeOnOrBefore(TimeUtils.ToEndDateTime(containingAncestor.Timestamp, containingAncestor.Duration));

		internal static void ShouldOccurBetween(this ITimedDto child, ITimedDto containingAncestor)
		{
			((ITimestampedDto)child).ShouldOccurBetween(containingAncestor);

			TimeUtils.ToEndDateTime(child.Timestamp, child.Duration)
				.Should()
				.BeOnOrBefore(TimeUtils.ToEndDateTime(containingAncestor.Timestamp, containingAncestor.Duration));
		}

		internal static void ShouldOccurBefore(this ITimedDto first, ITimestampedDto second) =>
			TimeUtils.ToEndDateTime(first.Timestamp, first.Duration).Should().BeOnOrBefore(TimeUtils.ToDateTime(second.Timestamp));
	}
}
