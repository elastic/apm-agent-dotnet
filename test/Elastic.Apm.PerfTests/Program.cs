using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Elastic.CommonSchema.BenchmarkDotNetExporter;

namespace Elastic.Apm.PerfTests
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var esUrl = Environment.GetEnvironmentVariable("ES_URL");
			var esPassword = Environment.GetEnvironmentVariable("ES_PASS");
			var esUser = Environment.GetEnvironmentVariable("ES_USER");

			ElasticsearchBenchmarkExporterOptions options;

			if (!string.IsNullOrEmpty(esUrl) && !string.IsNullOrEmpty(esPassword) && !string.IsNullOrEmpty(esUser))
			{
				Console.WriteLine($"Setting ElasticsearchBenchmarkExporterOptions based on environment variables - es URL: {esUrl}");
				options = new ElasticsearchBenchmarkExporterOptions(esUrl) { Username = esUser, Password = esPassword };
			}
			else
			{
				Console.WriteLine(
					"Setting ElasticsearchBenchmarkExporterOptions to export data to http://localhost:9200 - to change this set following environment variables: ES_URL, ES_PASS, ES_USER");
				options = new ElasticsearchBenchmarkExporterOptions("http://localhost:9200");
			}

			var exporter = new ElasticsearchBenchmarkExporter(options);
			var config = DefaultConfig.Instance.With(exporter);
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
		}
	}
}
