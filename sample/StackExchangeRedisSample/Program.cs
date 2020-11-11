using System;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.StackExchange.Redis;
using StackExchange.Redis;

namespace StackExchangeRedisSample
{
	class Program
	{
		private static async Task Main(string[] args)
		{
			// requires redis to be running on localhost:6379, e.g. with docker
			// docker run -p 6379:6379 --name some-redis -d redis
			var connection = await ConnectionMultiplexer.ConnectAsync("localhost");
			connection.UseElasticApm();

			for (var i = 0; i < 10; i++)
			{
				// async
				await Agent.Tracer.CaptureTransaction("Set and Get String", ApiConstants.TypeDb, async () =>
				{
					var database = connection.GetDatabase();
					await database.StringSetAsync($"string{i}", i);
					await database.StringGetAsync($"string{i}");

					// fire and forget commands do not appear in profiling sessions
					await database.StringSetAsync($"string{i}", i, flags: CommandFlags.FireAndForget);
					await database.StringGetAsync($"string{i}", CommandFlags.FireAndForget);
				});
			}

			// Allow the payload sender time to send to APM server.
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
		}
	}
}
