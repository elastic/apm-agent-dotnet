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

		public Error(CapturedException capturedException, string traceId, string transactionId, string parentId, Transaction transaction)
			: this(capturedException)
		{
			TraceId = traceId;
			TransactionId = transactionId;
			ParentId = parentId;
			Transaction = new TransactionData(transaction.IsSampled, transaction.Type);
		}

		private Error(CapturedException capturedException)
		{
			_start = DateTimeOffset.UtcNow;
			var idBytes = new byte[8];
			RandomGenerator.GenerateRandomBytes(idBytes);
			Id = BitConverter.ToString(idBytes).Replace("-", "");

			Exception = capturedException;
		}

		public Context Context { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Culprit { get; set; }

		public CapturedException Exception { get; set; }

		public string Id { get; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		// ReSharper disable once UnusedMember.Global, ImpureMethodCallOnReadonlyValueField
		public long Timestamp => _start.ToUnixTimeMilliseconds() * 1000;

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public TransactionData Transaction { get; }

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
