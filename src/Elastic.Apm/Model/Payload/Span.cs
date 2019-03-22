﻿using System;
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
		private readonly Lazy<SpanContext> _context = new Lazy<SpanContext>();
		private readonly IApmLogger _logger;
		private readonly IPayloadSender _payloadSender;

		private readonly DateTimeOffset _start;

		public Span(string name, string type, Transaction transaction, IPayloadSender payloadSender, IApmLogger logger)
		{
			_start = DateTimeOffset.UtcNow;
			_payloadSender = payloadSender;
			_logger = logger?.Scoped(nameof(Span));
			Name = name;
			Type = type;

			Id = RandomGenerator.GetRandomBytesAsString(new byte[8]);
			ParentId = transaction.Id;
			TransactionId = transaction.Id;
			TraceId = transaction.TraceId;
		}

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Action { get; set; }

		/// <summary>
		/// Any other arbitrary data captured by the agent, optionally provided by the user.
		/// </summary>
		public SpanContext Context => _context.Value;

		/// <inheritdoc />
		/// <summary>
		/// The duration of the span.
		/// If it's not set (HasValue returns false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		public double? Duration { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		[JsonProperty("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Subtype { get; set; }

		[JsonIgnore]
		public Dictionary<string, string> Tags => Context.Tags;

		//public decimal Start { get; set; }
		public long Timestamp => _start.ToUnixTimeMilliseconds() * 1000;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("transaction_id")]
		public string TransactionId { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }

		public override string ToString() =>
			$"Span, Id: {Id}, TransactionId: {TransactionId}, ParentId: {ParentId}, TraceId:{TraceId}, Name: {Name}, Type: {Type}";

		public void End()
		{
			_logger.Debug()?.Log("Ending {SpanDetails}" , ToString());
			if (!Duration.HasValue) Duration = (DateTimeOffset.UtcNow - _start).TotalMilliseconds;
			_payloadSender.QueueSpan(this);
		}

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null)
		{
			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;

			var ed = new CapturedException
			{
				Message = exception.Message,
				Type = exception.GetType().FullName,
				Handled = isHandled,
				Stacktrace = StacktraceHelper.GenerateApmStackTrace(exception, _logger,
					$"{nameof(Span)}.{nameof(CaptureException)}")
			};

			_payloadSender.QueueError(new Error(ed, TraceId, Id, parentId ?? Id) { Culprit = capturedCulprit /*, Context = Context */ });
		}

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null)
		{
			var capturedCulprit = string.IsNullOrEmpty(culprit) ? "PublicAPI-CaptureException" : culprit;

			var capturedException = new CapturedException()
			{
				Message = message,
			};

			if (frames != null)
			{
				capturedException.Stacktrace
					= StacktraceHelper.GenerateApmStackTrace(frames, _logger, $"{nameof(Span)}.{nameof(CaptureError)}");
			}

			_payloadSender.QueueError(
				new Error(capturedException, TraceId, Id, parentId ?? Id) { Culprit = capturedCulprit /*, Context = Context */ });
		}
	}
}
