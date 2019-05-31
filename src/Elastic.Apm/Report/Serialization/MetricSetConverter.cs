using System;
using Elastic.Apm.Metrics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.Report.Serialization
{
	public class MetricSetConverter: JsonConverter<Metrics.MetricSet>
	{
		public override void WriteJson(JsonWriter writer, Metrics.MetricSet value, JsonSerializer serializer)
		{
			var metrics = new JObject();
			var samples = new JObject();

			foreach (var item in value.Samples)
			{
				var valueObj = new JObject();
				valueObj.Add("value", item.KeyValue.Value);
				samples.Add(item.KeyValue.Key, valueObj);
			}

			metrics.Add("samples", samples);
			metrics.Add("timestamp", value.TimeStamp);

			metrics.WriteTo(writer);
		}

		public override Metrics.MetricSet ReadJson(JsonReader reader, Type objectType, Metrics.MetricSet existingValue, bool hasExistingValue, JsonSerializer serializer) => throw new NotImplementedException();
	}
}
