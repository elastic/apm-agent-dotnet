using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class TrimmedStringJsonConverter : JsonConverter<string>
	{
		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) => writer.WriteValue(value.TrimToMaxLength());

		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer) =>
			reader.Value as string;
	}
}
