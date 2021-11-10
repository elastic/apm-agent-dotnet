// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CommandLine;
using Elastic.Apm.Profiler.Managed.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Elastic.Apm.Profiler.IntegrationsGenerator
{
	internal class Program
	{
		private static void Main(string[] args) =>
			new Parser(cfg => cfg.CaseInsensitiveEnumValues = true)
				.ParseArguments<CommandLineOptions>(args)
				.MapResult(
					opts => Run(opts),
					errs => 1);

		private static int Run(CommandLineOptions opts)
		{
			try
			{
				// assumes the assembly is compatible to load for the TFM of this executable
				var targetAssembly = Assembly.LoadFrom(opts.Input);

				var classesInstrumentMethodAttributes =
					from wrapperType in targetAssembly.GetTypes()
					let attributes = wrapperType.GetCustomAttributes(false)
						.OfType<InstrumentAttribute>()
						.Select(a =>
						{
							a.CallTargetType = wrapperType;
							return a;
						})
						.ToList()
					from attribute in attributes
					select attribute;

				var callTargetIntegrations = from attribute in classesInstrumentMethodAttributes
					let integrationName = attribute.Group
					let assembly = attribute.CallTargetType.Assembly
					let wrapperType = attribute.CallTargetType
					orderby integrationName
					group new { assembly, wrapperType, attribute } by integrationName
					into g
					select new Integration
					{
						Name = g.Key,
						MethodReplacements = from item in g
							select new MethodReplacement
							{
								Target = new Target
								{
									Nuget = item.attribute.Nuget,
									Assembly = item.attribute.Assembly,
									Type = item.attribute.Type,
									Method = item.attribute.Method,
									SignatureTypes = new[] { item.attribute.ReturnType }
										.Concat(item.attribute.ParameterTypes ?? Enumerable.Empty<string>())
										.ToArray(),
									MinimumVersion = item.attribute.MinimumVersion,
									MaximumVersion = item.attribute.MaximumVersion
								},
								Wrapper = new Wrapper
								{
									Assembly = item.assembly.FullName,
									Type = item.wrapperType.FullName,
									Action = "CallTargetModification"
								}
							}
					};

				string output;
				switch (opts.Format)
				{
					case CommandLineOptions.OutputFormat.Yml:
						var serializer = new SerializerBuilder()
							.WithNamingConvention(UnderscoredNamingConvention.Instance)
							.Build();
						output = serializer.Serialize(callTargetIntegrations);
						break;
					case CommandLineOptions.OutputFormat.Asciidoc:
						output = GenerateAsciidoc(callTargetIntegrations);
						break;
					default: throw new ArgumentOutOfRangeException("format","Unknown format");
				}

				var filename = Path.Combine(opts.Output, "integrations." + opts.Format.ToString().ToLowerInvariant());
				File.WriteAllText(filename, output, new UTF8Encoding(false));
				return 0;
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e);
				return 1;
			}
		}

		private static string GenerateAsciidoc(IEnumerable<Integration> integrations)
		{
			var builder = new StringBuilder();
			builder
				.AppendLine(":star: *")
				.AppendLine(":nuget: https://www.nuget.org/packages")
				.AppendLine()
				.AppendLine("|===")
				.AppendLine("|**Integration name** |**NuGet package version(s)** |**Assembly version(s)** ");

			foreach (var integration in integrations)
			{
				var integrationMethods =
					integration.MethodReplacements
						.GroupBy(m => (m.Target.Nuget, m.Target.Assembly, m.Target.MinimumVersion, m.Target.MaximumVersion))
						.ToList();

				builder.AppendLine($".{integrationMethods.Count}+| {integration.Name}");

				foreach (var integrationMethod in integrationMethods)
				{
					var versionRange = $"{integrationMethod.Key.MinimumVersion.Replace("*", "{star}")} - "
						+ $"{integrationMethod.Key.MaximumVersion.Replace("*", "{star}")}";

					var nuget = integrationMethod.Key.Nuget is not null
						? integrationMethod.Key.Nuget.StartsWith("part of")
							? integrationMethod.Key.Nuget
							: $"{{nuget}}/{integrationMethod.Key.Nuget}[{integrationMethod.Key.Nuget} {versionRange}]"
						: $"{{nuget}}/{integrationMethod.Key.Assembly}[{integrationMethod.Key.Assembly} {versionRange}]";

					builder
						.AppendLine($"| {nuget}")
						.AppendLine($"| {integrationMethod.Key.Assembly} {versionRange}")
						.AppendLine();
				}
			}

			builder.AppendLine("|===");
			return builder.ToString();
		}
	}
}
