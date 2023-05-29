// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using CommandLine;

namespace Elastic.Apm.Profiler.IntegrationsGenerator
{
	public class CommandLineOptions
	{
		[Option('i', "input", Required = true, HelpText = "The input path to the managed assembly containing integrations")]
		public string Input { get; set; }

		[Option('o', "output", Required = false, HelpText = "The output directory for the generated integrations file", Default = "")]
		public string Output { get; set; }

		[Option('f', "format", Required = false, HelpText = "The output format for the generated integrations file")]
		public OutputFormat Format { get; set; }

		public enum OutputFormat
		{
			Yml,
			Asciidoc
		}
	}
}
