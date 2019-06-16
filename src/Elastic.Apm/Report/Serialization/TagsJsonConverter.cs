using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class TagsJsonConverter : JsonConverter<Dictionary<string, string>>
	{
		public override void WriteJson(JsonWriter writer, Dictionary<string, string> tags, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			foreach (var keyValue in tags)
			{
				writer.WritePropertyName(keyValue.Key.TrimToMaxLength());
				writer.WriteValue(keyValue.Value.TrimToMaxLength());
			}
			writer.WriteEndObject();
		}

		public override Dictionary<string, string> ReadJson(JsonReader reader, Type objectType, Dictionary<string, string> existingValue,
			bool hasExistingValue, JsonSerializer serializer
		)
			=> serializer.Deserialize<Dictionary<string, string>>(reader);
	}
}
