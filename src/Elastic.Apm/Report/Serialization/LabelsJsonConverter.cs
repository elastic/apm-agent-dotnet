// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class LabelsJsonConverter : JsonConverter<LabelsDictionary>
	{
		public override void WriteJson(JsonWriter writer, LabelsDictionary labels, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			foreach (var keyValue in labels.MergedDictionary)
			{
				// Labels are trimmed and also de dotted in order to satisfy the Intake API
				writer.WritePropertyName(keyValue.Key.Truncate()
					.Replace('.', '_')
					.Replace('*', '_')
					.Replace('"', '_'));

				if (keyValue.Value != null)
				{
					switch (keyValue.Value.Value)
					{
						case string strValue:
							writer.WriteValue(strValue.Truncate());
							break;
						default:
							writer.WriteValue(keyValue.Value.Value);
							break;
					}
				}
				else
					writer.WriteNull();
			}
			writer.WriteEndObject();
		}

		public override LabelsDictionary ReadJson(JsonReader reader, Type objectType, LabelsDictionary existingValue,
			bool hasExistingValue, JsonSerializer serializer
		)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType != JsonToken.StartObject)
				throw new JsonException($"expected {JsonToken.StartObject} but received {reader.TokenType}");

			var labels = new LabelsDictionary();

			while (reader.Read())
			{
				if (reader.TokenType == JsonToken.EndObject)
					break;

				var property = (string)reader.Value;
				reader.Read();

				switch (reader.TokenType)
				{
					case JsonToken.Integer:
						labels.InnerDictionary.Add(property, (long)reader.Value);
						break;
					case JsonToken.Float:
						labels.InnerDictionary.Add(property, (double)reader.Value);
						break;
					case JsonToken.String:
						labels.Add(property, (string)reader.Value);
						break;
					case JsonToken.Boolean:
						labels.InnerDictionary.Add(property, (bool)reader.Value);
						break;
					default:
						throw new JsonException(
							$"Expected {JsonToken.Integer}, {JsonToken.Float}, {JsonToken.String} or {JsonToken.Boolean} "
							+ $"but received {reader.TokenType}");
				}
			}

			return labels;
		}
	}
}
