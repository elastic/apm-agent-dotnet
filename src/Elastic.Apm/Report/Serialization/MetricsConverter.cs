using System;
using Elastic.Apm.Metrics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.Report.Serialization
{
	public class MetricsConverter: JsonConverter<Sample>

	{
		public override void WriteJson(JsonWriter writer, Sample value, JsonSerializer serializer)
		{
			var valueObj = new JObject();
			valueObj.Add("value", value.KeyValue.Value);

			var jObj = new JObject();
			jObj.Add(value.KeyValue.Key, valueObj);
			jObj.WriteTo(writer);
		}

		public override Sample ReadJson(JsonReader reader, Type objectType, Sample existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
	}
}
