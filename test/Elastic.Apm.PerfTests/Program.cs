using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Elastic.CommonSchema.BenchmarkDotNetExporter;

namespace Elastic.Apm.PerfTests
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var options = new ElasticsearchBenchmarkExporterOptions("http://localhost:9200");

			var exporter = new ElasticsearchBenchmarkExporter(options);
			var config = DefaultConfig.Instance.With(exporter);
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
		}
	}
}
