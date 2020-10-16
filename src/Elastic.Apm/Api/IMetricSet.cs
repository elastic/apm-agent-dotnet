// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api.Constraints;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Data captured by the agent representing a metric occurring in a monitored service
	/// </summary>
	[Specification("docs/spec/metricsets/metricset.json")]
	public interface IMetricSet
	{
		/// <summary>
		/// List of captured metrics as key - value pairs
		/// </summary>
		[Required]
		IEnumerable<MetricSample> Samples { get; set; }

		/// <summary>
		/// Number of milliseconds in unix time
		/// </summary>
		long Timestamp { get; set; }
	}
}
