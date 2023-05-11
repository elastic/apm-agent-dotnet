// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.MockApmServer
{
	public interface ITimedDto : ITimestampedDto
	{
		/// <summary>
		/// Duration in milliseconds, with a maximum of 3 decimal points
		/// </summary>
		double Duration { get; }

		internal DateTimeOffset EndDateTimeOffset() =>
			//timestamp µs + duration in ms
			TimestampUtils.ToDateTimeOffset(Timestamp + DurationUtils.Round(Duration * 1000));

	}

}
