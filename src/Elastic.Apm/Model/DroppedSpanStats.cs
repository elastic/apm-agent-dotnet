// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Elastic.Apm.Api;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// DroppedSpanStats holds information about spans that were dropped (for example due to transaction_max_spans or
	/// exit_span_min_duration).
	/// </summary>
	internal class DroppedSpanStats
	{
		public DroppedSpanStats(string destinationServiceResource, Outcome outcome, double durationSumUs)
		{
			DurationCount = 1;
			DestinationServiceResource = destinationServiceResource;
			Outcome = outcome;
			DurationSumUs = durationSumUs;
		}

		/// <summary>
		/// DestinationServiceResource identifies the destination service resource being operated on. e.g. 'http://elastic.co:80',
		/// 'elasticsearch', 'rabbitmq/queue_name'.
		/// </summary>
		[JsonProperty("destination_service_resource")]
		public string DestinationServiceResource { get; }

		/// <summary>
		/// Duration holds duration aggregations about the dropped span.
		/// Count holds the number of times the dropped span happened.
		/// </summary>
		[JsonProperty("duration.count")]
		public int DurationCount { get; set; }


		/// <summary>
		/// Duration holds duration aggregations about the dropped span.
		/// Sum holds dimensions about the dropped span's duration.
		/// </summary>
		[JsonProperty("duration.sum.us")]
		public double DurationSumUs { get; set; }

		/// <summary>
		/// Outcome of the aggregated spans.
		/// </summary>
		public Outcome Outcome { get; }
	}
}
