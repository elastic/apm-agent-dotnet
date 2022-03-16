// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;
using Elastic.Apm.Model;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class DbSpanNameTests
{
	[MemberData(nameof(SqlSignatureExamplesTestData))]
	[Theory]
	public void TestDbSignatures(string input, string output)
	{
		var signatureParser =  new SignatureParser(new Scanner());
		var name = new StringBuilder();
		signatureParser.QuerySignature(input.Replace(Environment.NewLine, " "), name,  preparedStatement: input.Length > 0);

		name.ToString().ToLower().Should().Be(output.ToLower());
	}

	public static IEnumerable<object[]> SqlSignatureExamplesTestData
	{
		get
		{
			var _projectRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var filePath= Path.GetFullPath(Path.Combine(_projectRoot, "TestResources" + Path.DirectorySeparatorChar + "db" + Path.DirectorySeparatorChar + "sql_signature_examples.json"));

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
