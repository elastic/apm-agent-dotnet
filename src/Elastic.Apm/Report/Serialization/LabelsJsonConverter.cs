// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
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
				writer.WritePropertyName(keyValue.Key.Truncate()
					.Replace('.', '_')
					.Replace('*', '_')
					.Replace('"', '_'));

				if (keyValue.Value != null)
					writer.WriteValue(keyValue.Value.Truncate());
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
