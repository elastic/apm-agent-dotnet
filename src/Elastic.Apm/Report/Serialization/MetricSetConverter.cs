using System;
using Elastic.Apm.Metrics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.Report.Serialization
{
	internal class MetricSetConverter : JsonConverter<MetricSet>
	{
		public override void WriteJson(JsonWriter writer, MetricSet value, JsonSerializer serializer)
		{
			var metrics = new JObject();
			var samples = new JObject();

			foreach (var item in value.Samples)
			{
				var valueObj = new JObject();
				valueObj.Add("value", item.KeyValue.Value);

				if(!samples.ContainsKey(item.KeyValue.Key))
					samples.Add(item.KeyValue.Key, valueObj);
			}

			metrics.Add("samples", samples);
			metrics.Add("timestamp", value.TimeStamp);

			metrics.WriteTo(writer);
		}

		public override MetricSet ReadJson(JsonReader reader, Type objectType, MetricSet existingValue, bool hasExistingValue,
			JsonSerializer serializer) => null; //unused
	}
}
