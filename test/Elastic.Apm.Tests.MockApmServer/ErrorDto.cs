using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Newtonsoft.Json;
// ReSharper disable MemberCanBePrivate.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class ErrorDto
	{
		public ContextDto Context { get; set; }
		public string Culprit { get; set; }
		public CapturedException Exception { get; set; }
		public string Id { get; set; }

		[JsonProperty("parent_id")] public string ParentId { get; set; }
		public long Timestamp { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public TransactionDataDto Transaction { get; }

		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(ErrorDto))
		{
			{ "Id", Id },
			{ "TraceId", TraceId },
			{ "ParentId", ParentId },
			{ "TransactionId", TransactionId },
			{ "Exception", Exception },
			{ "Culprit", Culprit },
			{ "Timestamp", Timestamp },
			{ "Transaction", Transaction },
			{ "Context", Context },
		}.ToString();


		public class TransactionDataDto
		{
			[JsonProperty("sampled")]
			public bool IsSampled { get; set; }

			public string Type { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(ErrorDto))
			{
				{ "Type", Type },
				{ "IsSampled", IsSampled },
			}.ToString();
		}
	}
}
