using System;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Metrics.MetricsProvider;
using Elastic.Apm.Tests.Mocks;
using Elastic.CommonSchema.BenchmarkDotNetExporter;

namespace Elastic.Apm.PerfTests
{
	public class Program
	{
		public static void Main(string[] args)
		{

			var options = new ElasticsearchBenchmarkExporterOptions("http://localhost:9200");

			var exporter = new ElasticsearchBenchmarkExporter(options);
			var config =  DefaultConfig.Instance.With(exporter);
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
		}
	}
}
