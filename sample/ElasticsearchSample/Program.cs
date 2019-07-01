using System;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.Elasticsearch;
using Elastic.Managed.Ephemeral;
using Elasticsearch.Net;
using Nest;

namespace ElasticsearchSample
{
	public static class Program
	{
		private static async Task Main(string[] args)
		{
			Agent.Subscribe(new ElasticsearchDiagnosticsSubscriber());


			var clusterConfiguration = new EphemeralClusterConfiguration("7.0.0")
			{
				StartingPortNumber = 9202
			};
			using (var cluster = new EphemeralCluster(clusterConfiguration))
			{
				//starts the cluster and waits 3 minutes for confirmation that it started
				cluster.Start(TimeSpan.FromMinutes(3));

				var connection = new SniffingConnectionPool(cluster.NodesUris());
				var settings = new ConnectionSettings(connection)
					.EnableDebugMode();
				var client = new ElasticClient(settings);

				//warmup
				await Agent.Tracer.CaptureTransaction("Warmup", ApiConstants.TypeRequest, async () =>
					await client.SearchAsync<object>(new SearchRequest())
				);

				for (var i = 0; i < 10; i++)
				{
					//async
					await Agent.Tracer.CaptureTransaction("Async Call", ApiConstants.TypeRequest, async () =>
						await client.SearchAsync<object>(new SearchRequest())
					);

					//sync
					Agent.Tracer.CaptureTransaction("SyncCall", ApiConstants.TypeRequest, () =>
						client.Search<object>(new SearchRequest())
					);
				}
			}
			await Task.Delay(TimeSpan.FromSeconds(3));

			Console.WriteLine("Finished running elasticsearch and issuing searches through the .NET client");
		}
	}
}
