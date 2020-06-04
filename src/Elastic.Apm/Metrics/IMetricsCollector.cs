// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

namespace Elastic.Apm.Metrics
{
	/// <summary>
	/// Defines how the agent collects metrics.
	/// </summary>
	internal interface IMetricsCollector
	{
		/// <summary>
		/// After calling this method, the <see cref="IMetricsCollector" /> starts collecting metrics
		/// </summary>
		void StartCollecting();
	}
}
