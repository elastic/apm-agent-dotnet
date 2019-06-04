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
		List<Sample> Samples { get; set; }

		/// <summary>
		/// Number of milliseconds in unix time
		/// </summary>
		long TimeStamp { get; set; }
	}
}
