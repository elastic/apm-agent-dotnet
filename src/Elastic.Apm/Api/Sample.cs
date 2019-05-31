using System.Collections.Generic;
using Elastic.Apm.Helpers;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// A single metric sample.
	/// </summary>
	public class Sample
	{
		public Sample(string key, double value)
			=> KeyValue = new KeyValuePair<string, double>(key, value);

		internal KeyValuePair<string, double> KeyValue { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Sample)) { { KeyValue.Key, KeyValue.Value }, }.ToString();
	}
}
