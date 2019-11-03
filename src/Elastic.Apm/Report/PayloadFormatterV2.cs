using System.Text;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Metrics;
using Elastic.Apm.Model;
using Elastic.Apm.Report.Serialization;

namespace Elastic.Apm.Report
{
	internal class PayloadFormatterV2 : IPayloadFormatter
	{
		private readonly IApmLogger _logger;
		private readonly PayloadItemSerializer _payloadItemSerializer;
		private readonly Metadata _metadata;

		private string _cachedMetadataJsonLine;

		public PayloadFormatterV2(IApmLogger logger, IConfigurationReader config, Metadata metadata)
		{
			_logger = logger.Scoped(nameof(PayloadFormatterV2));
			_payloadItemSerializer = new PayloadItemSerializer(config);
			_metadata = metadata;
		}

		public string FormatPayload(object[] items)
		{
			var ndjson = new StringBuilder();
			if (_cachedMetadataJsonLine == null)
				_cachedMetadataJsonLine = "{\"metadata\": " + _payloadItemSerializer.SerializeObject(_metadata) + "}";
			ndjson.AppendLine(_cachedMetadataJsonLine);

			foreach (var item in items)
			{
				var serialized = _payloadItemSerializer.SerializeObject(item);
				switch (item)
				{
					case Transaction _:
						ndjson.AppendLine("{\"transaction\": " + serialized + "}");
						break;
					case Span _:
						ndjson.AppendLine("{\"span\": " + serialized + "}");
						break;
					case Error _:
						ndjson.AppendLine("{\"error\": " + serialized + "}");
						break;
					case MetricSet _:
						ndjson.AppendLine("{\"metricset\": " + serialized + "}");
						break;
				}
				_logger?.Trace()?.Log("Serialized item to send: {ItemToSend} as {SerializedItem}", item, serialized);
			}

			return ndjson.ToString();
		}
	}
}
