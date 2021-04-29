// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;

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
