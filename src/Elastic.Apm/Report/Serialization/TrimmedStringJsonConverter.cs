using System;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class TrimmedStringJsonConverter : JsonConverter<string>
	{
		private readonly int _maxLength;

		// ReSharper disable once UnusedMember.Global
		public TrimmedStringJsonConverter() : this(Consts.PropertyMaxLength) { }

		// ReSharper disable once MemberCanBePrivate.Global
		public TrimmedStringJsonConverter(int maxLength) => _maxLength = maxLength;

		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer) =>
			writer.WriteValue(SerializationUtils.TrimToLength(value, _maxLength));

		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue, JsonSerializer serializer) =>
			reader.Value as string;
	}
}
