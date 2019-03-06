using System;
using System.Collections.Generic;
using Elastic.Apm.Api;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Error : IError
	{
		public string Id { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		public ExceptionDetails Exception { get; set; }

		public string Culprit { get; set; }

		public Context Context { get; set; }

		public Error(ExceptionDetails exceptionDetails, string traceId, string transactionId, string parentId) : this(exceptionDetails) =>
			(TraceId, TransactionId, ParentId) = (traceId, transactionId, parentId);

		public Error(ExceptionDetails exceptionDetails)
		{
			var rnd = new Random();
			Id = rnd.Next().ToString("X");
			Exception = exceptionDetails;
		}
	}


	public class ExceptionDetails
	{
		public int Code { get; set; }
		public string Message { get; set; }
		public List<StackFrame> Stacktrace { get; set; }
		public string Type { get; set; }
		public bool Handled { get; set; }
	}
}
