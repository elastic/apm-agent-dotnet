// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using FluentAssertions;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	[Specification("transaction.json")]
	internal class TransactionDto : ITimedDto
	{
		public ContextDto Context { get; set; }
		public double Duration { get; set; }
		public string Id { get; set; }

		[JsonProperty("sampled")]
		public bool IsSampled { get; set; }

		public string Name { get; set; }

		public Outcome Outcome { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		public string Result { get; set; }

		[JsonProperty("sample_rate")]
		public string SampleRate { get; set; }

		[JsonProperty("span_count")]
		public SpanCountDto SpanCount { get; set; }

		public long Timestamp { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public string Type { get; set; }


		[JsonProperty("dropped_spans_stats")]
		public List<DroppedSpanStatsDto> DroppedSpanStats { get; set; }

		public FaasDto FaaS { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(TransactionDto))
		{
			{ nameof(Id), Id },
			{ nameof(TraceId), TraceId },
			{ nameof(ParentId), ParentId },
			{ nameof(Name), Name },
			{ nameof(Type), Type },
			{ nameof(IsSampled), IsSampled },
			{ nameof(Timestamp), Timestamp },
			{ nameof(Duration), Duration },
			{ nameof(SpanCountDto), SpanCount },
			{ nameof(Result), Result },
			{ nameof(Context), Context }
		}.ToString();

		public void AssertValid()
		{
			Timestamp.TimestampAssertValid();
			Id.TransactionIdAssertValid();
			TraceId.TraceIdAssertValid();
			ParentId?.ParentIdAssertValid();
			SpanCount.AssertValid();
			Context?.AssertValid();
			Duration.DurationAssertValid();
			Name?.NameAssertValid();
			Result?.AssertValid();
			Type?.AssertValid();

			if (IsSampled)
				Context.Should().NotBeNull();
			else
				Context.Should().BeNull();
		}
	}
}
