using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.StackExchange.Redis;
using StackExchange.Redis;
using Testcontainers.Redis;

// handle control+c gracefully
var ctx = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
	eventArgs.Cancel = true;
	ctx.Cancel();
};

await using var container = new RedisBuilder().Build();
await container.StartAsync();

var connection = await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString());
connection.UseElasticApm();

for (var i = 0; i < 1_000_000; i++)
{
	if (ctx.IsCancellationRequested)
	{
		Console.WriteLine("\n Cancellation Requested.. halting...");
		break;
	}
	Console.Write($"\rExecuting iteration: {i}");
	// async
	await Agent.Tracer.CaptureTransaction("Set and Get String", ApiConstants.TypeDb, async () =>
	{
		var database = connection.GetDatabase();
		await database.StringSetAsync($"string{i}", i);
		await database.StringGetAsync($"string{i}");

		// fire and forget commands may not end up in the profiling session before
		// transaction end, and the profiling session is finished.
		await database.StringSetAsync($"string{i}", i, flags: CommandFlags.FireAndForget);
		await database.StringGetAsync($"string{i}", CommandFlags.FireAndForget);
	});
	await Task.Delay(TimeSpan.FromMilliseconds(100));
}
Console.WriteLine("Stopping Redis Container...");
await container.StopAsync(ctx.Token);
Console.WriteLine("Exiting");
