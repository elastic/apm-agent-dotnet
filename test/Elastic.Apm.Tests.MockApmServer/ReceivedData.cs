using System.Collections.Generic;

namespace Elastic.Apm.Tests.MockApmServer
{
	public class ReceivedData
	{
		internal readonly List<ErrorDto> Errors = new List<ErrorDto>();
		internal readonly List<string> InvalidPayloadErrors = new List<string>();
		internal readonly List<MetricSetDto> Metrics = new List<MetricSetDto>();
		internal readonly List<SpanDto> Spans = new List<SpanDto>();
		internal readonly List<TransactionDto> Transactions = new List<TransactionDto>();
		internal readonly List<MetadataDto> Metadata = new List<MetadataDto>();

		internal void Clear()
		{
			Errors.Clear();
			InvalidPayloadErrors.Clear();
			Metrics.Clear();
			Spans.Clear();
			Transactions.Clear();
			Metadata.Clear();
		}
	}
}
