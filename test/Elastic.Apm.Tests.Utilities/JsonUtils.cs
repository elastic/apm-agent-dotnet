// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Elastic.Apm.Tests.Utilities;

public static class JsonUtils
{
	public static string PrettyFormat(this string inputJson)
	{
		var node = JsonNode.Parse(inputJson);

		using var stream = new MemoryStream();
		using var jsonTextWriter = new Utf8JsonWriter(stream);
		node?.WriteTo(jsonTextWriter, new JsonSerializerOptions
		{
			WriteIndented = true
		});

		return Encoding.UTF8.GetString(stream.ToArray());

	}
}
