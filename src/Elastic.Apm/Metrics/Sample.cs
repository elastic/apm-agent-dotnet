using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Metrics
{
	public class Sample
	{
		public Sample(string key, double value)
			=> KeyValue = new KeyValuePair<string, double>(key, value);

		internal KeyValuePair<string, double> KeyValue { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Sample)) { { KeyValue.Key, KeyValue.Value }, }.ToString();
	}
}
