using System.Collections.Generic;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class ReceivedData
	{
		internal readonly List<ErrorDto> Errors = new List<ErrorDto>();
		internal readonly List<string> InvalidPayloadErrors = new List<string>();
		internal readonly List<MetadataDto> Metadata = new List<MetadataDto>();
		internal readonly List<MetricSetDto> Metrics = new List<MetricSetDto>();
		internal readonly List<SpanDto> Spans = new List<SpanDto>();
		internal readonly List<TransactionDto> Transactions = new List<TransactionDto>();
	}
}
