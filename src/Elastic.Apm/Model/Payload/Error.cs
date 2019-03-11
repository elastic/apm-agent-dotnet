using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Helpers;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Error : IError
	{
		public Error(CapturedException capturedException, string traceId, string transactionId, string parentId) : this(capturedException) =>
			(TraceId, TransactionId, ParentId) = (traceId, transactionId, parentId);

		public Error(CapturedException capturedException)
		{
			var idBytes = new byte[8];
			RandomGenerator.GetRandomBytes(idBytes);
			Id = BitConverter.ToString(idBytes).Replace("-","");

			Exception = capturedException;
		}

		public Context Context { get; set; }

		public string Culprit { get; set; }

		public CapturedException Exception { get; set; }

		public string Id { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		public override string ToString() => $"Error, Id: {Id}, TraceId: {TraceId}, ParentId: {ParentId}, TransactionId: {TransactionId}";
	}
}
