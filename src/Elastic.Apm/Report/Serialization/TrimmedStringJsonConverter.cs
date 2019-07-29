using System;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class TrimmedStringJsonConverter : JsonConverter<string>
	{
		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) =>
			writer.WriteValue(SerializationUtils.TrimToPropertyMaxLength(value));

		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer) =>
			reader.Value as string;
	}
}
