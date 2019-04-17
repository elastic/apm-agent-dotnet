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

		public Error(CapturedException capturedException, string traceId, string transactionId, string parentId) : this(capturedException) =>
			(TraceId, TransactionId, ParentId) = (traceId, transactionId, parentId);

		public Error(CapturedException capturedException)
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

		public long Timestamp => _start.ToUnixTimeMilliseconds() * 1000;

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		public override string ToString() => new ToStringBuilder(nameof(Error))
		{
			{"Id", Id},
			{"TraceId", TraceId},
			{"ParentId", ParentId},
			{"TransactionId", TransactionId}
		}.ToString();
	}
}
