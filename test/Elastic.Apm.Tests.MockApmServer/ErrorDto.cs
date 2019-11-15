using Elastic.Apm.Helpers;
using Elastic.Apm.Model;
using FluentAssertions;
using Newtonsoft.Json;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Elastic.Apm.Tests.MockApmServer
{
	internal class ErrorDto : ITimestampedDto
	{
		public ContextDto Context { get; set; }

		public string Culprit { get; set; }

		public CapturedException Exception { get; set; }

		public string Id { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		public long Timestamp { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public TransactionDataDto Transaction { get; set; }

		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(ErrorDto))
		{
			{ nameof(Id), Id },
			{ nameof(TraceId), TraceId },
			{ nameof(ParentId), ParentId },
			{ nameof(TransactionId), TransactionId },
			{ nameof(Exception), Exception },
			{ nameof(Culprit), Culprit },
			{ nameof(Timestamp), Timestamp },
			{ nameof(Transaction), Transaction },
			{ nameof(Context), Context }
		}.ToString();

		public void AssertValid()
		{
			Id.ErrorIdAssertValid();
			TraceId.TraceIdAssertValid();
			TransactionId.TransactionIdAssertValid();
			ParentId.ParentIdAssertValid();
			Transaction.AssertValid();
			Context?.AssertValid();
			Culprit.NonEmptyAssertValid();
			Exception.AssertValid();

			if (Transaction.IsSampled)
				Context.Should().NotBeNull();
			else
				Context.Should().BeNull();
		}

		public class TransactionDataDto : IDto
		{
			[JsonProperty("sampled")]
			public bool IsSampled { get; set; }

			public string Type { get; set; }

			public override string ToString() => new ToStringBuilder(nameof(ErrorDto)) { { "Type", Type }, { "IsSampled", IsSampled } }.ToString();

			public void AssertValid() => Type?.AssertValid();
		}
	}
}
