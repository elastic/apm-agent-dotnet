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
