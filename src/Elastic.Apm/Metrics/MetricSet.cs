using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	[JsonConverter(typeof(MetricSetConverter))]
	public class MetricSet : IMetricSet

	{
	public MetricSet(long timeStamp, List<Sample> samples)
		=> (TimeStamp, Samples) = (timeStamp, samples);

	public List<Sample> Samples { get; set; }

	[JsonProperty("timestamp")]
	public long TimeStamp { get; set; }
	}
}
