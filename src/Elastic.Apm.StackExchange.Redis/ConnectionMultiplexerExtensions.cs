using StackExchange.Redis;

namespace Elastic.Apm.StackExchange.Redis
{
	/// <summary>
	/// Extension methods for <see cref="IConnectionMultiplexer"/>
	/// </summary>
	public static class ConnectionMultiplexerExtensions
	{
		/// <summary>
		/// Register Elastic APM .NET Agent to capture profiled commands sent to redis
		/// </summary>
		/// <param name="connection">The connection to capture profiled commands for.</param>
		/// <param name="agent">The APM agent instance to register.</param>
		public static void UseElasticApm(this IConnectionMultiplexer connection, IApmAgent agent)
		{
			var profiler = new ElasticApmProfiler(agent);
			connection.RegisterProfiler(profiler.GetProfilingSession);
		}

		/// <summary>
		/// Register Elastic APM .NET Agent to capture profiled commands sent to redis
		/// </summary>
		/// <param name="connection">The connection to capture profiled commands for.</param>
		public static void UseElasticApm(this IConnectionMultiplexer connection)
		{
			var profiler = new ElasticApmProfiler(Agent.Instance);
			connection.RegisterProfiler(profiler.GetProfilingSession);
		}
	}
}
