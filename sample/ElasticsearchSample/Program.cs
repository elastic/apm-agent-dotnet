using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.Elasticsearch;
using Elasticsearch.Net;
using Microsoft.Extensions.Configuration;
using Nest;

namespace ElasticsearchSample
{
	public class Program
	{
		private class MyDocument
		{
			public string Id { get; set; }
		}

		private static ConnectionSettings DefaultConnectionSettings(ConnectionSettings s) => s
			.DefaultIndex("index")
			.DisableDirectStreaming();

		private static async Task Main(string[] args)
		{
			Agent.Subscribe(new ElasticsearchDiagnosticsSubscriber());

			// using var cluster = CreateEphemeralClient(out var connectionPool);
			var client = CreateLocalClient();

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
					await client.SearchAsync<object>(new SearchRequest("unknown-index")
					{
						//Size = int.MaxValue
					})
				);
				Console.WriteLine($"..Bad Request.... ({r.ApiCall.HttpStatusCode})");
			}
		}

		/// <summary>
		/// Create a client that connects to cloud, uses dotnet user-secrets.
		/// </summary>
		private static ElasticClient CreateCloudClient()
		{
			var userSecrets = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
			var basicAuth = new BasicAuthenticationCredentials(userSecrets["cloud:user"], userSecrets["cloud:password"]);
			var connectionPool = new CloudConnectionPool(userSecrets["cloud:id"], basicAuth);
			var settings = DefaultConnectionSettings(new ConnectionSettings(connectionPool));
			return new ElasticClient(settings);
		}

		/// <summary>
		/// Create a client that connects against a cluster started with: https://github.com/elastic/apm-integration-testing
		/// </summary>
		private static ElasticClient CreateLocalClient()
		{
			var connectionPool = new StaticConnectionPool(new[] { new Uri("http://localhost:9200") });
			var settings = DefaultConnectionSettings(new ConnectionSettings(connectionPool))
				.BasicAuthentication("admin", "changeme");
			return new ElasticClient(settings);
		}
	}
};
