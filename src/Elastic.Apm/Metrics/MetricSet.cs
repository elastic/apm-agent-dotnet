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
	public class MetricSet : IMetricSet
	{
		public MetricSet(long timestamp, IEnumerable<MetricSample> samples)
			=> (Timestamp, Samples) = (timestamp, samples);

		/// <inheritdoc />
		public IEnumerable<MetricSample> Samples { get; set; }

		/// <inheritdoc />
		public long Timestamp { get; set; }
	}
}
