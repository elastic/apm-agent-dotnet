using System;
using Elastic.Apm.Helpers;
using Elastic.Apm.Tests.MockApmServer;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public struct TimedEvent: ITimedDto
	{
		public TimedEvent(DateTime start, DateTime end)
		{
			Timestamp = TimeUtils.ToTimestamp(start);
			Duration = TimeUtils.DurationBetweenTimestamps(Timestamp, TimeUtils.ToTimestamp(end));

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
