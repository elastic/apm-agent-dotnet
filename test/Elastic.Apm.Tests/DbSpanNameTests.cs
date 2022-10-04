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
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;

namespace Elastic.Apm.Tests;

public class DbSpanNameTests
{
	[MemberData(nameof(SqlSignatureExamplesTestData))]
	[Theory]
	public void TestDbSignatures(string input, string output)
	{
		var signatureParser = new SignatureParser(new Scanner());
		var name = new StringBuilder();
		signatureParser.QuerySignature(input.Replace(Environment.NewLine, " "), name, false);

		name.ToString().ToLower().Should().Be(output.ToLower());
	}

	public static IEnumerable<object[]> SqlSignatureExamplesTestData
	{
		get
		{
			var _projectRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var filePath = Path.GetFullPath(Path.Combine(_projectRoot, "TestResources" + Path.DirectorySeparatorChar + "db" + Path.DirectorySeparatorChar + "sql_signature_examples.json"));

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

	public class SqlTokenTestData
	{
		public string Name;
		public string Comment;
		public string Input;
		public List<SqlToken> Tokens = new();
	}

	public struct SqlToken
	{
		public string Kind;
		public string Text;
	}

	[Theory]
	[JsonFileData("./TestResources/json-specs/sql_token_examples.json", typeof(SqlTokenTestData))]
	public void TestSqlTokenParsing(SqlTokenTestData data)
	{
		var parsedTokens = Helper_ParseSql(data.Input);
		parsedTokens.Count.Should().Be(data.Tokens.Count);
		for (var i = 0; i < parsedTokens.Count; i++)
			parsedTokens[i].ToString().Should().BeEquivalentTo(data.Tokens[i].Kind);
	}

	private static List<Scanner.Token> Helper_ParseSql(string sql)
	{
		var scanner = new Scanner();
		scanner.SetQuery(sql);
		var tokens = new List<Scanner.Token>();
		while (true)
		{
			var token = scanner.Scan();
			if (token == Scanner.Token.Eof)
				break;
			tokens.Add(token);
		}

		return tokens;
	}
}
