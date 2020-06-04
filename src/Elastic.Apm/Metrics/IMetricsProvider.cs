// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics
{
	/// <summary>
	/// Defines an interface that every class, which provides some metric value, should implement.
	/// This interface is known to the <see cref="MetricsCollector" /> type and you
	/// can implement new providers for other metrics by implementing this interface
	/// and adding it to <see cref="MetricsCollector" />.
	/// </summary>
	internal interface IMetricsProvider
	{
		/// <summary>
		/// Stores the number of calls to the <see cref="GetSamples" /> method when it returned null, an empty list or
		/// any of values is either NaN or Infinite. This is used by <see cref="MetricsCollector" />
		/// </summary>
		int ConsecutiveNumberOfFailedReads { get; set; }

		/// <summary>
		/// The name that refers to the provider in the logs. E.g. "total process CPU time".
		/// Make sure this is human understandable and tells the reader what type of value this provider is intended to provide.
		/// </summary>
		string DbgName { get; }

		/// <summary>
		/// The main part of the provider, the implementor should do the work to read the value(s) of the given metric(s) in this
		/// method.
		/// </summary>
		/// <returns>The key and the value of the metric(s)</returns>
		IEnumerable<MetricSample> GetSamples();

		/// <summary>
		/// Indicates if metrics were already collected - or there was an attempt to collect them.
		/// Until this property is false, metrics from the implementor won't be collected.
		/// This property exists to cover cases when the metric collection happens in the background
		/// (e.g. collecting GC metrics through EventListener) and values are not captured directly in
		/// the <see cref="GetSamples"/> method.
		/// If metrics are captured on the fly in <see cref="GetSamples"/> just set this to <code>true</code>
		/// during initialization.
		/// </summary>
		bool IsMetricAlreadyCaptured { get; }
	}
}
