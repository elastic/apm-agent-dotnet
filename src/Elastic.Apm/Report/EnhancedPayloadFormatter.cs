using System.IO;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Report
{
	internal class EnhancedPayloadFormatter : IPayloadFormatter
	{
		private readonly Metadata _metadata;

		private string _cachedMetadataJsonLine;

		private readonly StringWriterPool _stringWriterPool;

		internal JsonSerializerSettings Settings { get; }
		private readonly JsonSerializer _jsonSerializer;

		public EnhancedPayloadFormatter(IConfigurationReader config, Metadata metadata)
		{
			_metadata = metadata;

			_stringWriterPool = new StringWriterPool(5, 1_000, 100_000);

			Settings = new JsonSerializerSettings
			{
				ContractResolver = new ElasticApmContractResolver(config),
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.None,
			};
			_jsonSerializer = JsonSerializer.CreateDefault(Settings);
		}

		public string FormatPayload(object[] items)
		{
			//todo: after upgrade CI to .Net Core 3.0, need to be marked as static to forbid variable closure
			void WriteItem(string name, object item, TextWriter textWriter, JsonSerializer jsonSerializer)
			{
				textWriter.Write($"{{\"{name}\":");

				using (var jsonWriter = new JsonTextWriter(textWriter) { CloseOutput = false })
					jsonSerializer.Serialize(jsonWriter, item);

				textWriter.WriteLine("}");
			}

			if (_cachedMetadataJsonLine == null)
				_cachedMetadataJsonLine = "{\"metadata\":" + JsonConvert.SerializeObject(_metadata, Settings) + "}";

			using (var holder = _stringWriterPool.Get())
			{
				var writer = holder.Object;

				writer.WriteLine(_cachedMetadataJsonLine);

				foreach (var item in items)
				{
					switch (item)
					{
						case Transaction _:
							WriteItem("transaction", item, writer, _jsonSerializer);
							break;
						case Span _:
							WriteItem("span", item, writer, _jsonSerializer);
							break;
						case Error _:
							WriteItem("error", item, writer, _jsonSerializer);
							break;
						case MetricSet _:
							WriteItem("metricset", item, writer, _jsonSerializer);
							break;
					}
				}

				return writer.ToString();
			}
		}
	}
}
