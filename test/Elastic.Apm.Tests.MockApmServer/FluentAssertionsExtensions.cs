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
			TimestampUtils.ToDateTimeOffset(child.Timestamp)
				.Should()
				.BeOnOrAfter(containingAncestor.StartDateTimeOffset())
				.And
				.BeOnOrBefore(containingAncestor.EndDateTimeOffset());

		internal static void ShouldOccurBetween(this ITimedDto child, ITimedDto containingAncestor)
		{
			((ITimestampedDto)child).ShouldOccurBetween(containingAncestor);

			child.EndDateTimeOffset().Should().BeOnOrBefore(containingAncestor.EndDateTimeOffset());
		}

		internal static void ShouldOccurBefore(this ITimedDto first, ITimestampedDto second) =>
			first.EndDateTimeOffset().Should().BeOnOrBefore(second.StartDateTimeOffset());
	}
}
