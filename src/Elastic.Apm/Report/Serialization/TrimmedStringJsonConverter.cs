using System;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class TrimmedStringJsonConverter : JsonConverter<string>
	{
		private readonly int _maxLength;

		public TrimmedStringJsonConverter() : this(Consts.PropertyMaxLength) { }

		public TrimmedStringJsonConverter(int maxLength) => _maxLength = maxLength;

		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) =>
			writer.WriteValue(SerializationUtils.TrimToLength(value, _maxLength));

		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer) =>
			reader.Value as string;
	}
}
