using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Report.Serialization;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Newtonsoft.Json;

namespace Elastic.Apm.Model.Payload
{
	internal class Span : ISpan
	{
		private readonly Lazy<ContextImpl> _context = new Lazy<ContextImpl>();
		private readonly ScopedLogger _logger;
		private readonly IPayloadSender _payloadSender;

		private readonly DateTimeOffset _start;

		public Span(string name, string type, Transaction transaction, IPayloadSender payloadSender, IApmLogger logger)
		{
			_start = DateTimeOffset.Now;
			_payloadSender = payloadSender;
			_logger = logger?.Scoped(nameof(Span));
			Name = name;
			Type = type;

			var rnd = new Random();
			Id = rnd.Next().ToString("x");
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

		[JsonProperty("Stacktrace")]
		public List<StackFrame> StackTrace { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Subtype { get; set; }

		[JsonIgnore]
		public Dictionary<string, string> Tags => Context.Tags;

		//public decimal Start { get; set; }
		public long Timestamp => _start.ToUnixTimeMilliseconds() * 1000;

		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonProperty("Transaction_id")]
		public string TransactionId { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }

		public override string ToString() => $"Span, Id: {Id}, TransactionId: {TransactionId}, ParentId: {ParentId}, Name: {Name}, Type: {Type}";

		public void End()
		{
			_logger.LogDebug($"Ending {ToString()}");
			if (!Duration.HasValue) Duration = (DateTimeOffset.UtcNow - _start).TotalMilliseconds;
			_payloadSender.QueueSpan(this);
		}

		public void CaptureException(Exception exception, string culprit = null, string parentId = null)
		{
			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;

			var ed = new ExceptionDetails()
			{
				Message = exception.Message,
				Type = exception.GetType().FullName,
				//Handled = isHandled,
			};

			if (!string.IsNullOrEmpty(exception.StackTrace))
			{
				ed.Stacktrace
					= StacktraceHelper.GenerateApmStackTrace(new StackTrace(exception, true).GetFrames(), _logger,
						"failed capturing stacktrace");
			}

			_payloadSender.QueueError(new Error(ed, TraceId, Id, parentId ?? Id) { Culprit = capturedCulprit /*, Context = Context */ });
		}

		public void CaptureError(string message, string culprit, System.Diagnostics.StackFrame[] frames, string parentId = null)
		{
			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;

			var ed = new ExceptionDetails()
			{
				Message = message,
			};

			if (frames != null)
			{
				ed.Stacktrace
					= StacktraceHelper.GenerateApmStackTrace(frames, _logger, "failed capturing stacktrace");
			}

			_payloadSender.QueueError(new Error(ed, TraceId, Id, parentId ?? Id) { Culprit = capturedCulprit /*, Context = Context */ });
		}

		// ReSharper disable once ClassNeverInstantiated.Local - instantiated with Lazy<T>
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
