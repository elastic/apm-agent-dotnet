// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	public class LabelItemConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) => typeof(Label).IsAssignableFrom(objectType);

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			switch (reader.TokenType)
			{
				case JsonToken.Boolean:
					return new Label((bool)reader.Value);
				case JsonToken.Float:
					return new Label((double)reader.Value);
				case JsonToken.Integer:
					return new Label((long)reader.Value);
				case JsonToken.String:
					return new Label((string)reader.Value);
				default:
					throw new JsonException($"cannot deserialize {nameof(Label)} from token type {reader.TokenType}");
			}
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
			writer.WriteValue(((Label)value).Value);
	}
}
