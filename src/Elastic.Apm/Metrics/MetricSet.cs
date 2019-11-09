using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	[JsonConverter(typeof(MetricSetConverter))]
	internal class MetricSet : IMetricSet
	{
		public MetricSet(long timeStamp, IEnumerable<MetricSample> samples)
			=> (TimeStamp, Samples) = (timeStamp, samples.Where(x => !double.IsNaN(x.KeyValue.Value) && !double.IsInfinity(x.KeyValue.Value)));

		public IEnumerable<MetricSample> Samples { get; set; }

		[JsonProperty("timestamp")]
		public long TimeStamp { get; set; }
	}
}
