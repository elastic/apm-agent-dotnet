using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.Elasticsearch;
using Elastic.Managed.Ephemeral;
using Elasticsearch.Net;
using Nest;
using Context = Elastic.Apm.Api.Context;

namespace ElasticsearchSample
{
	public static class Program
	{
		public class MyDocument
		{
			public string Id { get; set; }
		}

		private static async Task Main(string[] args)
		{
			Agent.Subscribe(new ElasticsearchDiagnosticsSubscriber());

			var clusterConfiguration = new EphemeralClusterConfiguration("7.2.0")
			{
				StartingPortNumber = 9202,
				HttpFiddlerAware = true //Automatically routes to `ipv4.fiddler` on windows and
				// linux if Fiddler or mitmproxy is running (make sure ipv4.fiddler exists in hosts on linux)
			};
			using (var cluster = new EphemeralCluster(clusterConfiguration))
			{
				//starts the cluster and waits 3 minutes for confirmation that it started
				cluster.Start(TimeSpan.FromMinutes(3));

				var connection = new SniffingConnectionPool(cluster.NodesUris());
				var settings = new ConnectionSettings(connection)
					.DefaultIndex("index")
					.EnableDebugMode();
				var client = new ElasticClient(settings);

				// warm up
				await Agent.Tracer.CaptureTransaction("Warmup", ApiConstants.TypeDb, async () =>
					await client.SearchAsync<object>(new SearchRequest())
				);

				client.IndexDocument(new MyDocument { Id = "1" });

				for (var i = 0; i < 10; i++)
				{
					// async
					await Agent.Tracer.CaptureTransaction("Async Call", ApiConstants.TypeDb, async () =>
						await client.SearchAsync<object>(new SearchRequest())
					);
					Console.WriteLine("..Search Async....");

					// sync
					Agent.Tracer.CaptureTransaction("Sync Call", ApiConstants.TypeDb, () =>
						client.Search<object>(new SearchRequest())
					);
					Console.WriteLine("..Search Sync....");

					// send a request that fails server validation
					var r = await Agent.Tracer.CaptureTransaction("Bad Request", ApiConstants.TypeDb, async () =>
						await client.SearchAsync<object>(new SearchRequest()
						{
							Size = int.MaxValue
						})
					);
					Console.WriteLine($"..Bad Request.... ({r.ApiCall.HttpStatusCode})");
				}
			}
		}
	}
}
