using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class LabelsJsonConverter : JsonConverter<Dictionary<string, string>>
	{
		public override void WriteJson(JsonWriter writer, Dictionary<string, string> labels, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			foreach (var keyValue in labels)
			{
				// Labels are trimmed and also de dotted in order to satisfy the Intake API
				writer.WritePropertyName(SerializationUtils.TrimToPropertyMaxLength(keyValue.Key).Replace('.', '_'));

				if (keyValue.Value != null)
					writer.WriteValue(SerializationUtils.TrimToPropertyMaxLength(keyValue.Value));
				else
					writer.WriteNull();
			}
			writer.WriteEndObject();
		}

		public override Dictionary<string, string> ReadJson(JsonReader reader, Type objectType, Dictionary<string, string> existingValue,
			bool hasExistingValue, JsonSerializer serializer
		)
			=> serializer.Deserialize<Dictionary<string, string>>(reader);
	}
}
