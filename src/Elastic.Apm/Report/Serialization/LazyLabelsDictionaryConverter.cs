// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using Elastic.Apm.Model;
using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class LazyLabelsDictionaryConverter : JsonConverter<Lazy<LabelsDictionary>>
	{
		public override void WriteJson(JsonWriter writer, Lazy<LabelsDictionary> value, JsonSerializer serializer)
		{
			if (value.IsValueCreated) serializer.Serialize(writer, value.Value);
		}

		public override Lazy<LabelsDictionary> ReadJson(JsonReader reader, Type objectType, Lazy<LabelsDictionary> existingValue,
			bool hasExistingValue, JsonSerializer serializer
		) => serializer.Deserialize<Lazy<LabelsDictionary>>(reader);
	}
}
