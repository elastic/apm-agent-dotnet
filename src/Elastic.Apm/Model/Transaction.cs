﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class Transaction : ITransaction
	{
		private readonly Lazy<Context> _context = new Lazy<Context>();

		private readonly IApmLogger _logger;
		private readonly IPayloadSender _sender;

		private readonly DateTimeOffset _start;

		// This constructor is used only by tests that don't care about sampling and distributed tracing
		internal Transaction(IApmAgent agent, string name, string type)
			: this(agent.Logger, name, type, new Sampler(1.0), null, agent.PayloadSender) { }

		internal Transaction(
			IApmLogger logger,
			string name,
			string type,
			Sampler sampler,
			DistributedTracingData distributedTracingData,
			IPayloadSender sender
		)
		{
			_logger = logger?.Scoped(nameof(Transaction));
			_sender = sender;
			_start = DateTimeOffset.UtcNow;

			Name = name;
			Type = type;
			var idBytes = new byte[8];
			Id = RandomGenerator.GenerateRandomBytesAsString(idBytes);

			bool isSamplingFromDistributedTracingData = false;
			if (distributedTracingData == null)
			{
				var traceIdBytes = new byte[16];
				TraceId = RandomGenerator.GenerateRandomBytesAsString(traceIdBytes);
				IsSampled = sampler.DecideIfToSample(idBytes);
			}
			else
			{
				TraceId = distributedTracingData.TraceId;
				ParentId = distributedTracingData.ParentId;
				IsSampled = distributedTracingData.FlagRecorded;
				isSamplingFromDistributedTracingData = true;
			}

			SpanCount = new SpanCount();

			if (isSamplingFromDistributedTracingData)
				_logger.Trace()?.Log("New Transaction instance created: {Transaction}. " +
					"IsSampled ({IsSampled}) is based on incoming distributed tracing data ({DistributedTracingData})",
					this, IsSampled, distributedTracingData);
			else
				_logger.Trace()?.Log("New Transaction instance created: {Transaction}. " +
					"IsSampled ({IsSampled}) is based on the given sampler ({Sampler})",
					this, IsSampled, sampler);
		}

		/// <summary>
		/// Any arbitrary contextual information regarding the event, captured by the agent, optionally provided by the user.
		/// <seealso cref="ShouldSerializeContext" />
		/// </summary>
		public Context Context => _context.Value;

		/// <inheritdoc />
		/// <summary>
		/// The duration of the transaction.
		/// If it's not set (HasValue returns false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		public double? Duration { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; }

		[JsonProperty("sampled")]
		public bool IsSampled { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonIgnore]
		public DistributedTracingData OutgoingDistributedTracingData => new DistributedTracingData(TraceId, Id, IsSampled);

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		/// <inheritdoc />
		/// <summary>
		/// A string describing the result of the transaction.
		/// This is typically the HTTP status code, or e.g. "success" for a background task.
		/// </summary>
		/// <value>The result.</value>
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Result { get; set; }

		internal Service Service;

		[JsonProperty("span_count")]
		public SpanCount SpanCount { get; set; }

		[JsonIgnore]
		public Dictionary<string, string> Tags => Context.Tags;

		// ReSharper disable once ImpureMethodCallOnReadonlyValueField
		public long Timestamp => _start.ToUnixTimeMilliseconds() * 1000;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("trace_id")]
		public string TraceId { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }

		/// <summary>
		/// Method to conditionally serialize <see cref="Context" /> because context should be serialized only when the transaction
		/// is sampled.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeContext() => IsSampled;

		public override string ToString() => new ToStringBuilder(nameof(Transaction))
		{
			{ "Id", Id },
			{ "TraceId", TraceId },
			{ "ParentId", ParentId },
			{ "Name", Name },
			{ "Type", Type },
			{ "IsSampled", IsSampled }
		}.ToString();

		public void End()
		{
			if (!Duration.HasValue) Duration = (DateTimeOffset.UtcNow - _start).TotalMilliseconds;

			_sender.QueueTransaction(this);

			_logger.Debug()?.Log("Ending {TransactionDetails}", ToString());
			Agent.TransactionContainer.Transactions.Value = null;
		}

		public ISpan StartSpan(string name, string type, string subType = null, string action = null)
			=> StartSpanInternal(name, type, subType, action);

		internal Span StartSpanInternal(string name, string type, string subType = null, string action = null)
		{
			var retVal = new Span(name, type, Id, TraceId, this, IsSampled, _sender, _logger);

			if (!string.IsNullOrEmpty(subType)) retVal.Subtype = subType;

			if (!string.IsNullOrEmpty(action)) retVal.Action = action;

			_logger.Debug()?.Log("Starting {SpanDetails}", retVal.ToString());
			return retVal;
		}

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null)
			=> ExecutionSegmentCommon.CaptureException(
				exception,
				_logger,
				_sender,
				this,
				this,
				culprit,
				isHandled,
				parentId
			);

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null)
			=> ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_sender,
				_logger,
				this,
				this,
				parentId
			);

		public void CaptureSpan(string name, string type, Action<ISpan> capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), capturedAction);

		public void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), capturedAction);

		public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), func);

		public T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), func);

		public Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), func);

		public Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action), func);

		internal static string StatusCodeToResult(string protocolName, int StatusCode) => $"{protocolName} {StatusCode.ToString()[0]}xx";
	}
}
