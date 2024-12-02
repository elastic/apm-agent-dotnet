// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elastic.Apm.Api;

using Elastic.Apm.Metrics;

namespace Elastic.Apm.Report.Serialization
{
	internal class MetricSetConverter : JsonConverter<MetricSet>
	{
		public override void Write(Utf8JsonWriter writer, MetricSet value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			if (value.Transaction != null)
			{

				writer.WritePropertyName("transaction");

				writer.WriteStartObject();

				writer.WritePropertyName("name");
				writer.WriteStringValue(value.Transaction.Name);

				writer.WritePropertyName("type");
				writer.WriteStringValue(value.Transaction.Type);

				writer.WriteEndObject();
			}

			if (value.Span != null)
			{

				writer.WritePropertyName("span");

				writer.WriteStartObject();

				writer.WritePropertyName("type");
				writer.WriteStringValue(value.Span.Type);

				writer.WritePropertyName("subtype");
				writer.WriteStringValue(value.Span.SubType);

				writer.WriteEndObject();
			}


			writer.WritePropertyName("samples");
			writer.WriteStartObject();

			var addedKeys = new HashSet<string>();
			foreach (var item in value.Samples)
			{
				if (addedKeys.Add(item.KeyValue.Key))
				{
					writer.WritePropertyName(item.KeyValue.Key
						.Replace('*', '_')
						.Replace('"', '_'));
					writer.WriteStartObject();
					writer.WritePropertyName("value");
					writer.WriteNumberValue((decimal)item.KeyValue.Value);
					writer.WriteEndObject();
				}
			}

			writer.WriteEndObject();
			writer.WritePropertyName("timestamp");
			writer.WriteNumberValue(value.Timestamp);
			writer.WriteEndObject();
		}

		public override MetricSet Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Null)
				return null;

			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException($"Expected {JsonTokenType.StartObject} but found {reader.TokenType}");

			long timestamp = 0;
			var samples = new List<MetricSample>();

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					break;

				var property = reader.GetString();
				switch (property)
				{
					case "samples":
						reader.Read(); // {
						while (reader.Read())
						{
							if (reader.TokenType == JsonTokenType.EndObject)
								break;

							var key = reader.GetString();
							double value = 0;
							reader.Read(); // {
							while (reader.Read())
							{
								if (reader.TokenType == JsonTokenType.EndObject)
									break;

								var sampleValueProperty = reader.GetString();
								if (sampleValueProperty == "value")
								{
									reader.Read();
									value = reader.GetDouble();
								}
							}

							samples.Add(new MetricSample(key, value));
						}
						break;
					case "timestamp":
						reader.Read();
						timestamp = reader.GetInt64();
						break;
				}
			}

			return new MetricSet(timestamp, samples);
		}

	}
}
