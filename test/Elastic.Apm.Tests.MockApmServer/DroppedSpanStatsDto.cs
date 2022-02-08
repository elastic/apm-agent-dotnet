// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class DroppedSpanStatsDto
	{
		[JsonProperty("destination_service_resource")]
		public string DestinationServiceResource { get; }

		[JsonProperty("duration.count")]
		public int DurationCount { get; set; }

		[JsonProperty("duration.sum.us")]
		public double DurationSumUs { get; set; }

		public Outcome Outcome { get; }
	}
}
