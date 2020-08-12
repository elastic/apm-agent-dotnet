// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	[JsonConverter(typeof(MetricSetConverter))]
	internal class MetricSet : IMetricSet
	{
		public MetricSet(long timeStamp, IEnumerable<MetricSample> samples)
			=> (TimeStamp, Samples) = (timeStamp, samples);

		public IEnumerable<MetricSample> Samples { get; set; }

		[JsonProperty("timestamp")]
		public long TimeStamp { get; set; }
	}
}
