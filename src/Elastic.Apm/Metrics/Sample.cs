using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	[JsonConverter(typeof(MetricsConverter))]
	public class Sample
	{
		public Sample(string key, double value)
		 => KeyValue = new  KeyValuePair<string, double>(key,value);

		internal KeyValuePair<string, double> KeyValue { get; set; }
	}
}
