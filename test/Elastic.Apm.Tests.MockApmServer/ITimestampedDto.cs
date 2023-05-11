// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Tests.MockApmServer
{
	public interface ITimestampedDto : IDto
	{
		/// <summary> Timestamp in microseconds (µs) </summary>
		long Timestamp { get; }

		internal DateTimeOffset StartDateTimeOffset() => TimestampUtils.ToDateTimeOffset(Timestamp);

	}
}
