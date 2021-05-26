using System;
using Elastic.Apm.Api;
using Elastic.Apm.Libraries.Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class OutcomeConverter : JsonConverter<Outcome>
	{
		public override Outcome ReadJson(JsonReader reader, Type objectType, Outcome existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (hasExistingValue)
				return existingValue;

			while (reader.Read())
			{
				if (reader.TokenType == JsonToken.EndObject)
					break;

				if (reader.ValueType == typeof(string))
				{
					var property = (string)reader.Value;

					if (Enum.TryParse<Outcome>(property, out var enumVal))
					{
						return enumVal;
					}
				}
			}

			return Outcome.Unknown;
		}
		public override void WriteJson(JsonWriter writer, Outcome value, JsonSerializer serializer)
		{

			writer.WriteStartObject();
			writer.WriteValue(value.ToString().ToLower());
			writer.WriteEndObject();
		}
	}
}
