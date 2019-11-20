using System.Collections.Generic;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// A single metric sample.
	/// </summary>
	public class MetricSample
	{
		public MetricSample(string key, double value)
			=> KeyValue = new KeyValuePair<string, double>(key, value);

		internal KeyValuePair<string, double> KeyValue { get; }

		public override string ToString() => new ToStringBuilder(nameof(MetricSample)) { { KeyValue.Key, KeyValue.Value } }.ToString();
	}
}
