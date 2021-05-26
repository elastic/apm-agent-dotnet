// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Api;
using Elastic.Apm.Metrics;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Linq;

namespace Elastic.Apm.Report.Serialization
{
	internal class MetricSetConverter : JsonConverter<MetricSet>
	{
		public override void WriteJson(JsonWriter writer, MetricSet value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
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
					writer.WriteValue(item.KeyValue.Value);
					writer.WriteEndObject();
				}
			}

			writer.WriteEndObject();
			writer.WritePropertyName("timestamp");
			writer.WriteValue(value.Timestamp);
			writer.WriteEndObject();
		}

		public override MetricSet ReadJson(JsonReader reader, Type objectType, MetricSet existingValue, bool hasExistingValue,
			JsonSerializer serializer
		)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType != JsonToken.StartObject)
				throw new JsonReaderException($"Expected {JsonToken.StartObject} but found {reader.TokenType}");

			long timestamp = 0;
			var samples = new List<MetricSample>();

			while (reader.Read())
			{
				if (reader.TokenType == JsonToken.EndObject)
					break;

				var property = (string)reader.Value;
				switch (property)
				{
					case "samples":
						reader.Read(); // {
						while (reader.Read())
						{
							if (reader.TokenType == JsonToken.EndObject)
								break;

							var key = (string)reader.Value;
							double value = 0;
							reader.Read(); // {
							while (reader.Read())
							{
								if (reader.TokenType == JsonToken.EndObject)
									break;

								var sampleValueProperty = (string)reader.Value;
								if (sampleValueProperty == "value")
								{
									reader.Read();
									value = (double)reader.Value;
								}
							}

							samples.Add(new MetricSample(key, value));
						}
						break;
					case "timestamp":
						reader.Read();
						timestamp = (long)reader.Value;
						break;
				}
			}

			return new MetricSet(timestamp, samples);
		}
	}
}
