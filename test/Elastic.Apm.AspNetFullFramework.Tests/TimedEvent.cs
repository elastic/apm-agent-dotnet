// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.MockApmServer;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public struct TimedEvent : ITimedDto
	{
		public TimedEvent(DateTimeOffset start, DateTimeOffset end)
		{
			Timestamp = TimestampUtils.ToTimestamp(start);
			Duration = TimestampUtils.DurationBetweenTimestamps(Timestamp, TimestampUtils.ToTimestamp(end));

			AssertValid();
		}

		public long Timestamp { get; }
		public double Duration { get; }

		public void AssertValid()
		{
			Timestamp.TimestampAssertValid();
			Duration.DurationAssertValid();
		}
	}
}
