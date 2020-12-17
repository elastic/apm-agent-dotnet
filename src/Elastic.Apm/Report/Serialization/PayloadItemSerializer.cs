// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Newtonsoft.Json;

namespace Elastic.Apm.Report.Serialization
{
	internal class PayloadItemSerializer
	{
		private readonly JsonSerializerSettings _settings;

		internal PayloadItemSerializer() =>
			_settings = new JsonSerializerSettings
			{
				ContractResolver = new ElasticApmContractResolver(),
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.None,
			};

		internal string SerializeObject(object item) =>
			JsonConvert.SerializeObject(item, _settings);
	}
}
