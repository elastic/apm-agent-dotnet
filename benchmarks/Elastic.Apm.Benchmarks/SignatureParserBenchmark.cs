// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;

using Elastic.Apm.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.Benchmarks
{
	public class SignatureParserBenchmark
	{
		[Benchmark]
		public void TestDbSignatures()
		{
			var parserData = GetSqlSignatureExamplesTestData();
			foreach (var data in parserData)
			{
				var signatureParser = new SignatureParser(new Scanner());
				var name = new StringBuilder();
				signatureParser.QuerySignature((data[0] as string)?.Replace(Environment.NewLine, " "), name, false);
			}
		}

		private static IEnumerable<object[]> GetSqlSignatureExamplesTestData()
		{
			var projectRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var filePath = Path.GetFullPath(Path.Combine(projectRoot,
				"TestResources" + Path.DirectorySeparatorChar + "db" + Path.DirectorySeparatorChar +
				"sql_signature_examples.json"));

			using (var file = File.OpenText(filePath))
			using (var reader = new JsonTextReader(file))
			{
				var array = (JArray)JToken.ReadFrom(reader);

				foreach (var item in array)
				{
					var input = item["input"].Value<string>();
					var output = item["output"].Value<string>();

					yield return new object[] { input, output };
				}
			}
		}
	}
}
