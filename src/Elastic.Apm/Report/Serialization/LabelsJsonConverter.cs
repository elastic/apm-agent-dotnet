// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

using Elastic.Apm.Model;

namespace Elastic.Apm.Report.Serialization
{
	internal class LabelsJsonConverter : JsonConverter<LabelsDictionary>
	{
		public override void Write(Utf8JsonWriter writer, LabelsDictionary labels, JsonSerializerOptions options)
		{
			if (labels is null or { MergedDictionary.Count: 0 })
				return;

			writer.WriteStartObject();
			foreach (var keyValue in labels.MergedDictionary)
			{
				// Labels are trimmed and also de dotted in order to satisfy the Intake API
				var key = keyValue.Key
					.Truncate()
					.Replace('.', '_')
					.Replace('*', '_')
					.Replace('"', '_');

				writer.WritePropertyName(key);

				SerializeLabelsDictionaryValues(writer, keyValue);
			}
			writer.WriteEndObject();
		}

		private static void SerializeLabelsDictionaryValues(Utf8JsonWriter writer, KeyValuePair<string, Label> keyValue)
		{
			if (keyValue.Value != null)
			{
				switch (keyValue.Value.Value)
				{
					case string strValue:
						writer.WriteStringValue(strValue.Truncate());
						break;
					case bool b:
						writer.WriteBooleanValue(b);
						break;
					case decimal b:
						writer.WriteRawValue(b.ToString("N1", CultureInfo.InvariantCulture));
						break;
					case double b:
						writer.WriteRawValue(b.ToString("N1", CultureInfo.InvariantCulture));
						break;
					case int b:
						writer.WriteNumberValue(b);
						break;
					case uint b:
						writer.WriteNumberValue(b);
						break;
					case ulong b:
						writer.WriteNumberValue(b);
						break;
					case long b:
						writer.WriteNumberValue(b);
						break;
					default:
						writer.WriteNullValue();
						break;
				}
			}
			else
				writer.WriteNullValue();
		}

		public override LabelsDictionary Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null)
				return null;

			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException($"expected {JsonTokenType.StartObject} but received {reader.TokenType}");

			var labels = new LabelsDictionary();

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					break;

				var property = reader.GetString()!;
				reader.Read();

				switch (reader.TokenType)
				{
					case JsonTokenType.Number:
						if (reader.TryGetInt64(out var longValue))
							labels.InnerDictionary.Add(property, longValue);
						else if (reader.TryGetDouble(out var doubleValue))
							labels.InnerDictionary.Add(property, doubleValue);
						break;
					case JsonTokenType.String:
						labels.Add(property, reader.GetString());
						break;
					case JsonTokenType.True:
						labels.InnerDictionary.Add(property, true);
						break;
					case JsonTokenType.False:
						labels.InnerDictionary.Add(property, false);
						break;
					default:
						throw new JsonException(
							$"Expected {JsonTokenType.Number}, {JsonTokenType.String}, {JsonTokenType.True} or {JsonTokenType.False} "
							+ $"but received {reader.TokenType}");
				}
			}

			return labels;
		}

	}
}
