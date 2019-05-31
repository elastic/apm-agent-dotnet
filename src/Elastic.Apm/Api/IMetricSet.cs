using System.Collections.Generic;

namespace Elastic.Apm.Api
{
	/// <summary>
	/// Data captured by an agent representing an event occurring in a monitored service
	/// </summary>
	public interface IMetricSet
	{
		List<Sample> Samples { get; set; }

		long TimeStamp { get; set; }
	}
}
