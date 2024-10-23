// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Text.Json.Serialization;
using Elastic.Apm.Api;


namespace Elastic.Apm.Tests.MockApmServer
{
	public class DroppedSpanStatsDto
	{
		[JsonPropertyName("destination_service_resource")]
		public string DestinationServiceResource { get; }

		[JsonPropertyName("duration.count")]
		public int DurationCount { get; set; }

		[JsonPropertyName("duration.sum.us")]
		public double DurationSumUs { get; set; }

		public Outcome Outcome { get; }
	}
}
