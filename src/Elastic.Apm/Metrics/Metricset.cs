using System;
using System.Collections.Generic;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	public class MetricSet
	{
		public MetricSet(long timeStamp, Sample samples)
			=> (TimeStamp, Samples) = (timeStamp, samples);

		public Sample Samples { get; set; }

		[JsonProperty("timestamp")]
		public long TimeStamp { get; set; }
	}
}
