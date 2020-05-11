// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class Span : ISpan
	{
		private readonly Lazy<SpanContext> _context = new Lazy<SpanContext>();
		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;
		private readonly Transaction _enclosingTransaction;

		private readonly bool _isDropped;
		private readonly IApmLogger _logger;
		private readonly Span _parentSpan;
		private readonly IPayloadSender _payloadSender;

		/// <summary>
		/// In some cases capturing the stacktrace in <see cref="End"/> results in a stack trace which is not very useful.
		/// In such cases we capture the stacktrace on span start.
		/// These are typically async calls - e.g. capturing stacktrace for outgoing HTTP requests in the System.Net.Http.HttpRequestOut.Stop
		/// diagnostic source event produces a stack trace that does not contain the caller method in user code - therefore we capture the stacktrace is .Start
		/// </summary>
		private readonly StackFrame[] _stackFrames;

		// This constructor is meant for deserialization
		[JsonConstructor]
		private Span(double duration, string id, string name, string parentId)
		{
			Duration = duration;
			Id = id;
			Name = name;
			ParentId = parentId;
		}

		public Span(
			string name,
			string type,
			string parentId,
			string traceId,
			Transaction enclosingTransaction,
			IPayloadSender payloadSender,
			IApmLogger logger,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			Span parentSpan = null,
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None,
			bool captureStackTraceOnStart = false
		)
		{
			InstrumentationFlag = instrumentationFlag;
			Timestamp = TimeUtils.TimestampNow();
			Id = RandomGenerator.GenerateRandomBytesAsString(new byte[8]);
			_logger = logger?.Scoped($"{nameof(Span)}.{Id}");

			_payloadSender = payloadSender;
			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;
			_parentSpan = parentSpan;
			_enclosingTransaction = enclosingTransaction;
			Name = name;
			Type = type;

			ParentId = parentId;
			TraceId = traceId;

			if (IsSampled)
			{
				// Started and dropped spans should be counted only for sampled transactions
				if (enclosingTransaction.SpanCount.IncrementTotal() > ConfigSnapshot.TransactionMaxSpans
					&& ConfigSnapshot.TransactionMaxSpans >= 0)
				{
					_isDropped = true;
					enclosingTransaction.SpanCount.IncrementDropped();
				}
				else
				{
					enclosingTransaction.SpanCount.IncrementStarted();

					if(captureStackTraceOnStart)
						_stackFrames = new StackTrace(true).GetFrames();
				}
			}

			_currentExecutionSegmentsContainer.CurrentSpan = this;

			_logger.Trace()
				?.Log("New Span instance created: {Span}. Start time: {Time} (as timestamp: {Timestamp}). Parent span: {Span}",
					this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, _parentSpan);
		}

		private bool _isEnded;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Action { get; set; }

		[JsonIgnore]
		private IConfigSnapshot ConfigSnapshot => _enclosingTransaction.ConfigSnapshot;

		/// <summary>
		/// Any other arbitrary data captured by the agent, optionally provided by the user.
		/// <seealso cref="ShouldSerializeContext" />
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

		internal InstrumentationFlag InstrumentationFlag { get; }

		[JsonIgnore]
		public bool IsSampled => _enclosingTransaction.IsSampled;

		[JsonIgnore]
		public Dictionary<string, string> Labels => Context.Labels;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonIgnore]
		public DistributedTracingData OutgoingDistributedTracingData => new DistributedTracingData(
			TraceId,
			// When transaction is not sampled then outgoing distributed tracing data should have transaction ID for parent-id part
			// and not span ID as it does for sampled case.
			ShouldBeSentToApmServer ? Id : TransactionId,
			IsSampled);

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		[JsonIgnore]
		internal bool ShouldBeSentToApmServer => IsSampled && !_isDropped;

		[JsonProperty("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Subtype { get; set; }

		//public decimal Start { get; set; }

		/// <summary>
		/// Recorded time of the event, UTC based and formatted as microseconds since Unix epoch
		/// </summary>
		public long Timestamp { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("transaction_id")]
		public string TransactionId => _enclosingTransaction.Id;

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }

		/// <summary>
		/// Method to conditionally serialize <see cref="Context" /> - serialize only if it was accessed at least once.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeContext() => _context.IsValueCreated;

		public override string ToString() => new ToStringBuilder(nameof(Span))
		{
			{ nameof(Id), Id },
			{ nameof(TransactionId), TransactionId },
			{ nameof(ParentId), ParentId },
			{ nameof(TraceId), TraceId },
			{ nameof(Name), Name },
			{ nameof(Type), Type },
			{ nameof(IsSampled), IsSampled }
		}.ToString();

		public ISpan StartSpan(string name, string type, string subType = null, string action = null)
			=> StartSpanInternal(name, type, subType, action);

		internal Span StartSpanInternal(string name, string type, string subType = null, string action = null,
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None, bool captureStackTraceOnStart = false
		)
		{
			var retVal = new Span(name, type, Id, TraceId, _enclosingTransaction, _payloadSender, _logger, _currentExecutionSegmentsContainer, this,
				instrumentationFlag, captureStackTraceOnStart);

			if (!string.IsNullOrEmpty(subType)) retVal.Subtype = subType;

			if (!string.IsNullOrEmpty(action)) retVal.Action = action;

			_logger.Trace()?.Log("Starting {SpanDetails}", retVal.ToString());
			return retVal;
		}

		public void End()
		{
			if (Duration.HasValue)
			{
				_logger.Trace()
					?.Log("Ended {Span} (with Duration already set)." +
						" Start time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, Duration);
			}
			else
			{
				Assertion.IfEnabled?.That(!_isEnded,
					$"Span's Duration doesn't have value even though {nameof(End)} method was already called." +
					$" It contradicts the invariant enforced by {nameof(End)} method - Duration should have value when {nameof(End)} method exits" +
					$" and {nameof(_isEnded)} field is set to true only when {nameof(End)} method exits." +
					$" Context: this: {this}; {nameof(_isEnded)}: {_isEnded}");

				var endTimestamp = TimeUtils.TimestampNow();
				Duration = TimeUtils.DurationBetweenTimestamps(Timestamp, endTimestamp);
				_logger.Trace()
					?.Log("Ended {Span}. Start time: {Time} (as timestamp: {Timestamp})," +
						" End time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp,
						TimeUtils.FormatTimestampForLog(endTimestamp), endTimestamp, Duration);
			}

			var isFirstEndCall = !_isEnded;
			_isEnded = true;

			if (ShouldBeSentToApmServer && isFirstEndCall)
			{
				DeduceDestination();

				// Spans are sent only for sampled transactions so it's only worth capturing stack trace for sampled spans
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (ConfigSnapshot.StackTraceLimit != 0 && ConfigSnapshot.SpanFramesMinDurationInMilliseconds != 0)
				{
					if (Duration >= ConfigSnapshot.SpanFramesMinDurationInMilliseconds
						|| ConfigSnapshot.SpanFramesMinDurationInMilliseconds < 0)
					{
						StackTrace = StacktraceHelper.GenerateApmStackTrace(_stackFrames ?? new StackTrace(true).GetFrames(), _logger,
							ConfigSnapshot, $"Span `{Name}'");
					}
				}

				_payloadSender.QueueSpan(this);
			}

			if (isFirstEndCall) _currentExecutionSegmentsContainer.CurrentSpan = _parentSpan;
		}

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null)
			=> ExecutionSegmentCommon.CaptureException(
				exception,
				_logger,
				_payloadSender,
				this,
				ConfigSnapshot,
				_enclosingTransaction,
				culprit,
				isHandled,
				parentId ?? (ShouldBeSentToApmServer ? null : _enclosingTransaction.Id)
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

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null)
			=> ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_payloadSender,
				_logger,
				this,
				ConfigSnapshot,
				_enclosingTransaction,
				parentId ?? (ShouldBeSentToApmServer ? null : _enclosingTransaction.Id)
			);

		private void DeduceDestination()
		{
			if (!_context.IsValueCreated) return;

			if (Context.Http != null) CopyMissingProperties(DeduceHttpDestination());

			void CopyMissingProperties(Destination src)
			{
				if (src == null) return;

				if (Context.Destination == null)
					Context.Destination = src;
				else
					Context.Destination.CopyMissingPropertiesFrom(src);
			}
		}

		private Destination DeduceHttpDestination()
		{
			try
			{
				return UrlUtils.ExtractDestination(Context.Http.OriginalUrl ?? new Uri(Context.Http.Url), _logger);
			}
			catch (Exception ex)
			{
				_logger.Trace()
					?.LogException(ex, "Failed to deduce destination info from Context.Http."
						+ " Original URL: {OriginalUrl}. Context.Http.Url: {Context.Http.Url}."
						, Context.Http.OriginalUrl, Context.Http.Url);
				return null;
			}
		}
	}
}
