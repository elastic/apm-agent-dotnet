// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;
using Xunit.Sdk;

namespace Elastic.Apm.Tests.Utilities;

public class JsonFileDataAttribute : DataAttribute
{
	private readonly string _fileName;
	private readonly Type _inputDataType;

	public JsonFileDataAttribute(string fileName, Type inputDataType = null)
	{
		_fileName = fileName;
		_inputDataType = inputDataType ?? typeof(JToken);
	}

	public override IEnumerable<object[]> GetData(MethodInfo testMethod)
	{
		if (!File.Exists(_fileName))
			throw new ArgumentException($"JSON input file {_fileName} does not exist");

		var jToken = JToken.Parse(File.ReadAllText(_fileName), new JsonLoadSettings
		{
			CommentHandling = CommentHandling.Ignore
		});
		switch (jToken.Type)
		{
			case JTokenType.Array:
				foreach (var t in jToken)
					yield return new []{ t.ToObject(_inputDataType) };
				break;
			case JTokenType.Object:
				foreach (var kvp in (JObject)jToken)
					yield return new [] { kvp.Key, kvp.Value.ToObject(_inputDataType) };
				break;
			default:
				throw new Exception($"Unexpected JSON input: '{jToken}'");
		}
	}
}
