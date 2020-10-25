// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class LabelsJsonConverter : JsonConverter<Dictionary<string, Label>>
	{
		public override void WriteJson(JsonWriter writer, Dictionary<string, Label> labels, JsonSerializer serializer)
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
				{
					switch (keyValue.Value.Value)
					{
						case string strValue:
							writer.WriteValue(strValue.Truncate());
							break;
						default:
							writer.WriteValue(keyValue.Value.Value);
							break;
					}
				}
				else
					writer.WriteNull();
			}
			writer.WriteEndObject();
		}

		public override Dictionary<string, Label> ReadJson(JsonReader reader, Type objectType, Dictionary<string, Label> existingValue,
			bool hasExistingValue, JsonSerializer serializer
		)
			=> serializer.Deserialize<Dictionary<string, Label>>(reader);
	}
}
