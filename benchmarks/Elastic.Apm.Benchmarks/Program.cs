// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.IO;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Elastic.Apm.Benchmarks.Helpers;
using Elastic.CommonSchema.BenchmarkDotNetExporter;

namespace Elastic.Apm.Benchmarks
{
	public class Program
	{
		public static void Main(string[] args)
		{
			//var thing = new FreeAndTotalMemoryProviderBenchmarks();
			//thing.Setup();
			//thing.GetSamplesOriginal();
			//thing.Cleanup();

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

			using (var gitInfo = new GitInfo())
			{
				try
				{
					options.GitBranch = gitInfo.BranchName;
					options.GitCommitSha = gitInfo.CommitHash;
				}
				catch (Exception e)
				{
					Console.WriteLine("Failed reading git info");
					Console.WriteLine(e);
				}
			}

			//var exporter = new ElasticsearchBenchmarkExporter(options);
			var config = DefaultConfig.Instance
				//.AddExporter(exporter)
				.WithSummaryStyle(new SummaryStyle(null, false, null, null, ratioStyle: RatioStyle.Percentage));

			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
		}
	}
}
