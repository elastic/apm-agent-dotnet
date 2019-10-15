using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	/// <summary>
	/// Sanitizes HTTP headers based on the config passed to the constructor
	/// </summary>
	internal class HeaderDictionarySanitizerConverter : JsonConverter<Dictionary<string, string>>
	{
		private readonly IConfigurationReader _configurationReader;

		public HeaderDictionarySanitizerConverter(IConfigurationReader configurationReader)
			=> _configurationReader = configurationReader;

		public override void WriteJson(JsonWriter writer, Dictionary<string, string> headers, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			foreach (var keyValue in headers)
			{
				writer.WritePropertyName(SerializationUtils.TrimToPropertyMaxLength(keyValue.Key));

				if (keyValue.Value != null)
				{
					writer.WriteValue(WildcardMatcher.IsAnyMatch(_configurationReader.SanitizeFieldNames, keyValue.Key)
						? "[REDACTED]"
						: keyValue.Value);
				}
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
