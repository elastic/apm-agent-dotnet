using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Report.Serialization
{
	internal class PayloadItemSerializer
	{
		private readonly JsonSerializerSettings _settings;

		internal PayloadItemSerializer()
		{
			_settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver(), Formatting = Formatting.None };
		}

		internal string SerializeObject(object item)
		{
			return JsonConvert.SerializeObject(item, _settings);
		}
	}
}
