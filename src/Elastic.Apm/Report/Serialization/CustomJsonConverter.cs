// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Elastic.Apm.Report.Serialization;

internal class CustomJsonConverter : JsonConverter<Dictionary<string, string>>
{
	public override Dictionary<string, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
		JsonSerializer.Deserialize<Dictionary<string, string>>(ref reader, options);

	public override void Write(Utf8JsonWriter writer, Dictionary<string, string> value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var keyValue in value)
		{
			var key = keyValue.Key
				.Replace('.', '_')
				.Replace('*', '_')
				.Replace('"', '_');

			if (keyValue.Value != null)
				writer.WriteString(key, keyValue.Value);
			else
				writer.WriteNull(key);
		}
		writer.WriteEndObject();
	}
}

public class BooleanConverter : JsonConverter<bool>
{
	public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		switch (reader.TokenType)
		{
			case JsonTokenType.True:
				return true;
			case JsonTokenType.False:
				return false;
			case JsonTokenType.String:
				return reader.GetString() switch
				{
					"true" => true,
					"false" => false,
					_ => throw new JsonException()
				};
			default:
				throw new JsonException();
		}
	}

	public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
		writer.WriteBooleanValue(value);
}

public class JsonConverterDouble : JsonConverter<double>
{
	public override double Read(ref Utf8JsonReader reader,
		Type typeToConvert, JsonSerializerOptions options) => reader.GetDouble();

	public override void Write(Utf8JsonWriter writer, double value,
		JsonSerializerOptions options) =>
			writer.WriteRawValue(value.ToString("0.000", CultureInfo.InvariantCulture));
}

public class JsonConverterDecimal : JsonConverter<decimal>
{
	public override decimal Read(ref Utf8JsonReader reader,
		Type typeToConvert, JsonSerializerOptions options) =>
		reader.GetDecimal();

	public override void Write(Utf8JsonWriter writer, decimal value,
		JsonSerializerOptions options) =>
		writer.WriteRawValue(value.ToString("N1", CultureInfo.InvariantCulture));
}
