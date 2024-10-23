// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elastic.Apm.Report.Serialization;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.Utilities;

public class JsonFileDataAttribute : DataAttribute
{
	private readonly string _fileName;
	private readonly Type _inputDataType;

	public JsonFileDataAttribute(string fileName, Type inputDataType = null)
	{
		_fileName = fileName;
		_inputDataType = inputDataType ?? typeof(JsonObject);
	}

	public override IEnumerable<object[]> GetData(MethodInfo testMethod)
	{
		if (!File.Exists(_fileName))
			throw new ArgumentException($"JSON input file {_fileName} does not exist");

		var jToken = JsonNode.Parse(File.ReadAllText(_fileName), new JsonNodeOptions(), new JsonDocumentOptions
		{
			CommentHandling = JsonCommentHandling.Skip
		});
		switch (jToken)
		{
			case JsonArray jsonArray:
				foreach (var t in jsonArray)
					yield return [t.Deserialize(_inputDataType, PayloadItemSerializer.Default.Settings)];
				break;
			case JsonObject jsonObject:
				foreach (var kvp in jsonObject)
					yield return [kvp.Key, kvp.Value.Deserialize(_inputDataType, PayloadItemSerializer.Default.Settings)];
				break;
			default:
				throw new Exception($"Unexpected JSON input: '{jToken}'");
		}
	}
}
