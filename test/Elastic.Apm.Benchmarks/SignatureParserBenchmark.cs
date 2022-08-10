// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using Elastic.Apm.Model;
using Elastic.Apm.Tests;

namespace Elastic.Apm.Benchmarks
{
	public class SignatureParserBenchmark
	{
		[Benchmark]
		public void TestDbSignatures()
		{
			var parserData = DbSpanNameTests.SqlSignatureExamplesTestData;

			foreach (var data in parserData)
			{
				var signatureParser = new SignatureParser(new Scanner());
				var name = new StringBuilder();
				signatureParser.QuerySignature((data[0] as string)?.Replace(Environment.NewLine, " "), name, false);
			}
		}
	}
}
