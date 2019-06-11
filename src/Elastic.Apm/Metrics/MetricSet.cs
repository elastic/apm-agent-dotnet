using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	[JsonConverter(typeof(MetricSetConverter))]
	internal class MetricSet : IMetricSet
	{
		public MetricSet(long timeStamp, List<MetricSample> samples)
			=> (TimeStamp, Samples) = (timeStamp, samples);

		public IEnumerable<MetricSample> Samples { get; set; }

		[JsonProperty("timestamp")]
		public long TimeStamp { get; set; }
	}
}
