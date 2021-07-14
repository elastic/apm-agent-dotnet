using System;
using System.Net.Http;
using Elasticsearch.Net;

namespace Example
{
	internal static class Program
    {
		private static void Main(string[] args)
		{
			var client = new HttpClient();
			var uri = "https://elastic.co";
			var response = client.GetAsync(uri).Result;
			Console.WriteLine($"status code from {uri} is {response.StatusCode}");

			var elasticLowLevelClient = new ElasticLowLevelClient();
			var info = elasticLowLevelClient.Info<StringResponse>();
			Console.WriteLine(info);
		}
	}
}
