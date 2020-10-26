// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Data captured by the agent representing a metric occurring in a monitored service
	/// </summary>
	public interface IMetricSet
	{
		/// <summary>
		/// List of captured metrics as key - value pairs
		/// </summary>
		IEnumerable<MetricSample> Samples { get; set; }

		// TODO: Rename to Timestamp for consistency?
		/// <summary>
		/// Number of milliseconds in unix time
		/// </summary>
		long TimeStamp { get; set; }
	}
}
