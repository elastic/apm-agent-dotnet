using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global


namespace Elastic.Apm.Tests.MockApmServer
{
	internal class SpanDto : ITimedDto
	{
		public string Action { get; set; }
		public SpanContextDto Context { get; set; }
		public double Duration { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		[JsonProperty("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		public string Subtype { get; set; }

		public long Timestamp { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		public string Type { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(SpanDto))
		{
			{ "Id", Id },
			{ "TransactionId", TransactionId },
			{ "ParentId", ParentId },
			{ "TraceId", TraceId },
			{ "Name", Name },
			{ "Type", Type },
			{ "Subtype", Subtype },
			{ "Action", Action },
			{ "Timestamp", Timestamp },
			{ "Duration", Duration },
			{ "Context", Context }
		}.ToString();

		public void AssertValid()
		{
			Timestamp.TimestampAssertValid();
			Id.SpanIdAssertValid();
			TransactionId.TransactionIdAssertValid();
			TraceId.TraceIdAssertValid();
			ParentId.ParentIdAssertValid();
			Subtype?.AssertValid();
			Action?.AssertValid();
			Context?.AssertValid();
			Duration.DurationAssertValid();
			Name?.NameAssertValid();
			StackTrace?.AssertValid();
			Type?.AssertValid();
		}
	}
}
