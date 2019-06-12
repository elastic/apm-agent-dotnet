using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Newtonsoft.Json;
// ReSharper disable MemberCanBePrivate.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class TransactionDto
	{
		public ContextDto Context { get; set; }
		public double Duration { get; set; }
		public string Id { get; set; }

		[JsonProperty("sampled")]
		public bool IsSampled { get; set; }

		public string Name { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		public string Result { get; set; }

		[JsonProperty("span_count")]
		public SpanCount SpanCount { get; set; }

		public long Timestamp { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public string Type { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(TransactionDto))
		{
			{ "Id", Id },
			{ "TraceId", TraceId },
			{ "ParentId", ParentId },
			{ "Name", Name },
			{ "Type", Type },
			{ "IsSampled", IsSampled },
			{ "Timestamp", Timestamp },
			{ "Duration", Duration },
			{ "SpanCount", SpanCount },
			{ "Result", Result },
			{ "Context", Context }
		}.ToString();
	}
}
