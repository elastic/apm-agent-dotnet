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
		/// Stores the number of calls to the <see cref="GetSamples"/> method when it either returned null or an empty list.
		/// This is used by <see cref="MetricsCollector"/>
		/// </summary>
		int ConsecutiveNumberOfFailedReads { get; set; }

		/// <summary>
		/// The name that refers to the provider in the logs. E.g. "total process CPU time".
		/// Make sure this is human understandable and tells the reader what type of value this provider is intended to provide.
		/// </summary>
		string DbgName { get; }

		/// <summary>
		/// The main part of the provider, the implementor should do the work to read the value(s) of the given metric(s) in this method.
		/// </summary>
		/// <returns>The key and the value of the metric(s)</returns>
		IEnumerable<MetricSample> GetSamples();
	}
}
