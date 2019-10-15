using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class HeaderDictionarySanitizerConverter  : JsonConverter<Dictionary<string, string>>
	{
		private readonly IConfigurationReader _configurationReader;

		public HeaderDictionarySanitizerConverter(IConfigurationReader configurationReader)
		 => _configurationReader = configurationReader;

		public override void WriteJson(JsonWriter writer, Dictionary<string, string> labels, JsonSerializer serializer)
		{
			//TODO: sanitize here
			writer.WriteStartObject();
			foreach (var keyValue in labels)
			{
				writer.WritePropertyName(keyValue.Key);

				if (keyValue.Value != null)
					writer.WriteValue(keyValue.Value);
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
