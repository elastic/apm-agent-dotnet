using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	internal class PayloadItemSerializer
	{
		private readonly JsonSerializerSettings _settings;

		internal PayloadItemSerializer() =>
			_settings = new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.None
			};

		internal string SerializeObject(object item) =>
			JsonConvert.SerializeObject(item, _settings);
	}
}
