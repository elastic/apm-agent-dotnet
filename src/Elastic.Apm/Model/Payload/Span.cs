using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Report;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Span : ISpan
	{
		private readonly Lazy<ContextImpl> _context = new Lazy<ContextImpl>();

		private readonly DateTimeOffset _start;
		private IPayloadSender _payloadSender;

		public Span(string name, string type, Transaction transaction, IPayloadSender payloadSender)
		{
			//Transaction = transaction;
			_start = DateTimeOffset.UtcNow;
			_payloadSender = payloadSender;
			Start = (decimal)(_start - transaction.Start).TotalMilliseconds;
			Name = name;
			Type = type;

			Random rnd = new Random();
			Id = rnd.Next().ToString();
			ParentId = transaction.Id; //TODO
			TransactionId = transaction.Id;
			TraceId = transaction.TraceId; //TODO
		}

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Action { get; set; }

		/// <summary>
		/// Any other arbitrary data captured by the agent, optionally provided by the user.
		/// </summary>
		public IContext Context => _context.Value;

		/// <inheritdoc />
		/// <summary>
		/// The duration of the span.
		/// If it's not set (HasValue returns false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		public double? Duration { get; set; }

		public string Id { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonProperty("Stacktrace")]
		public List<Stacktrace> StackTrace { get; set; }

		public decimal Start { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Subtype { get; set; }

		[JsonIgnore]
		public Dictionary<string, string> Tags => Context.Tags;

		[JsonProperty("Transaction_id")]
		public string TransactionId { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }

		public void End()
		{
			if (!Duration.HasValue) Duration = (DateTimeOffset.UtcNow - _start).TotalMilliseconds;
			_payloadSender.QueueSpan(this);
			//Transaction?.SpansInternal.Add(this);
		}

		public void CaptureException(Exception exception, string culprit = null) { } //TODO: Transaction?.CaptureException(exception, culprit);

		public void
			CaptureError(string message, string culprit, StackFrame[] frames) { } //TODO: => Transaction?.CaptureError(message, culprit, frames);

		private class ContextImpl : IContext
		{
			private readonly Lazy<Dictionary<string, string>> _tags = new Lazy<Dictionary<string, string>>();
			public IDb Db { get; set; }
			public IHttp Http { get; set; }
			public Dictionary<string, string> Tags => _tags.Value;
		}
	}

	internal interface IContext
	{
		IDb Db { get; set; }
		IHttp Http { get; set; }
		[JsonConverter(typeof(TagsJsonConverter))]
		Dictionary<string, string> Tags { get; }
	}

	internal interface IDb
	{
		string Statement { get; set; }
		string Type { get; set; }
	}

	internal interface IHttp
	{
		string Method { get; set; }
		int StatusCode { get; set; }
		string Url { get; set; }
	}

	internal class Db : IDb
	{

		public string Instance { get; set; }
		public string Statement { get; set; }
		public string Type { get; set; }
	}

	internal class Http : IHttp
	{
		public string Method { get; set; }
		public int StatusCode { get; set; }
		public string Url { get; set; }
	}
}
