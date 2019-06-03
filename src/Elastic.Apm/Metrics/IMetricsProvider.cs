using System.Collections.Generic;
using Elastic.Apm.Api;

namespace Elastic.Apm.Metrics
{
	/// <summary>
	/// Defines an interface that every class implements that provides some metric value.
	/// This interface is known to the <see cref="MetricsCollector" /> type and you
	/// can implement new providers for other metrics by implementing this interface
	/// and adding it to <see cref="MetricsCollector" />
	/// </summary>
	internal interface IMetricsProvider
	{
		int ConsecutiveNumberOfFailedReads { get; set; }

		string NameInLogs { get; }

		IEnumerable<Sample> GetValue();
	}
}
