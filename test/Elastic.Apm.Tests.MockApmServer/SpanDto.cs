// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;

using Elastic.Apm.Model;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global


namespace Elastic.Apm.Tests.MockApmServer
{
	[Specification("span.json")]
	internal class SpanDto : ITimedDto
	{
		public string Action { get; set; }
		public SpanContextDto Context { get; set; }
		public double Duration { get; set; }
		public string Id { get; set; }
		public string Name { get; set; }

		public Outcome Outcome { get; set; }

		[JsonPropertyName("parent_id")]
		public string ParentId { get; set; }

		[JsonPropertyName("sample_rate")]
		public string SampleRate { get; set; }

		[JsonPropertyName("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		public List<SpanLinkDto> Links { get; set; }

		public OTel Otel { get; set; }

		public string Subtype { get; set; }

		public long Timestamp { get; set; }

		[JsonPropertyName("trace_id")]
		public string TraceId { get; set; }

		[JsonPropertyName("transaction_id")]
		public string TransactionId { get; set; }

		public string Type { get; set; }

		public CompositeDto Composite { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(SpanDto))
		{
			{ nameof(Id), Id },
			{ nameof(TransactionId), TransactionId },
			{ nameof(ParentId), ParentId },
			{ nameof(TraceId), TraceId },
			{ nameof(Name), Name },
			{ nameof(Type), Type },
			{ nameof(Subtype), Subtype },
			{ nameof(Action), Action },
			{ nameof(Timestamp), Timestamp },
			{ nameof(Duration), Duration },
			{ nameof(Context), Context }
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
			Links?.AssertValid();
			Type?.AssertValid();
		}
	}
}
