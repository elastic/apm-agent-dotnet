using System;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class Error : IError
	{
		private readonly DateTimeOffset _start;

		public Error(CapturedException capturedException, Transaction transaction, string parentId)
			: this(capturedException)
		{
			TraceId = transaction.TraceId;
			TransactionId = transaction.Id;
			ParentId = parentId;
			Transaction = new TransactionData(transaction.IsSampled, transaction.Type);
		}

		private Error(CapturedException capturedException)
		{
			_start = DateTimeOffset.UtcNow;
			Id = RandomGenerator.GenerateRandomBytesAsString( new byte[16]);
			Exception = capturedException;
		}

		public Context Context { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Culprit { get; set; }

		public CapturedException Exception { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		// ReSharper disable once UnusedMember.Global, ImpureMethodCallOnReadonlyValueField
		public long Timestamp => _start.ToUnixTimeMilliseconds() * 1000;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public TransactionData Transaction { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Error))
		{
			{ "Id", Id },
			{ "TraceId", TraceId },
			{ "ParentId", ParentId },
			{ "TransactionId", TransactionId },
		}.ToString();

		public class TransactionData
		{
			internal TransactionData(bool isSampled, string type)
			{
				IsSampled = isSampled;
				Type = type;
			}

			[JsonProperty("sampled")]
			public bool IsSampled { get; }

			[JsonConverter(typeof(TrimmedStringJsonConverter))]
			public string Type { get; }

			public override string ToString() => new ToStringBuilder(nameof(TransactionData))
			{
				{ "IsSampled", IsSampled },
				{ "Type", Type },
			}.ToString();
		}
	}
}
