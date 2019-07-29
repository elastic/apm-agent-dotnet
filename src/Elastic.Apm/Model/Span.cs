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
		private readonly IConfigurationReader _configurationReader;
		private readonly Lazy<SpanContext> _context = new Lazy<SpanContext>();
		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;
		private readonly Transaction _enclosingTransaction;
		private readonly IApmLogger _logger;
		private readonly Span _parentSpan;
		private readonly IPayloadSender _payloadSender;

		public Span(
			string name,
			string type,
			string parentId,
			string traceId,
			Transaction enclosingTransaction,
			bool isSampled,
			IPayloadSender payloadSender,
			IApmLogger logger,
			IConfigurationReader configurationReader,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			Span parentSpan = null
		)
		{
			Timestamp = TimeUtils.TimestampNow();
			Id = RandomGenerator.GenerateRandomBytesAsString(new byte[8]);
			_logger = logger?.Scoped($"{nameof(Span)}.{Id}");

			_payloadSender = payloadSender;
			_configurationReader = configurationReader;
			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;
			_parentSpan = parentSpan;
			_enclosingTransaction = enclosingTransaction;
			Name = name;
			Type = type;
			IsSampled = isSampled;

			ParentId = parentId;
			TraceId = traceId;

			if (IsSampled)
				// Started spans should be counted only for sampled transactions
				enclosingTransaction.SpanCount.Started++;

			_currentExecutionSegmentsContainer.CurrentSpan = this;

			_logger.Trace()
				?.Log("New Span instance created: {Span}. Start time: {Time} (as timestamp: {Timestamp}). Parent span: {Span}",
					this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, _parentSpan);
		}

		private bool _isEnded;

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

		[JsonIgnore]
		public bool IsSampled { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name { get; set; }

		[JsonIgnore]
		public DistributedTracingData OutgoingDistributedTracingData => new DistributedTracingData(
			TraceId,
			// When transaction is not sampled then outgoing distributed tracing data should have transaction ID for parent-id part
			// and not span ID as it does for sampled case.
			IsSampled ? Id : TransactionId,
			IsSampled);

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

		internal Span StartSpanInternal(string name, string type, string subType = null, string action = null)
		{
			var retVal = new Span(name, type, Id, TraceId, _enclosingTransaction, IsSampled, _payloadSender, _logger, _configurationReader,
				_currentExecutionSegmentsContainer, this);
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

			if (!IsSampled || !isFirstEndCall) return;

			// Spans are sent only for sampled transactions so it's only worth capturing stack trace for sampled spans
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (_configurationReader.StackTraceLimit != 0 && _configurationReader.SpanFramesMinDurationInMilliseconds != 0)
			{
				if (Duration >= _configurationReader.SpanFramesMinDurationInMilliseconds
					|| _configurationReader.SpanFramesMinDurationInMilliseconds < 0)
				{
					StackTrace = StacktraceHelper.GenerateApmStackTrace(new StackTrace(true).GetFrames(), _logger,
						_configurationReader, $"Span `{Name}'");
				}
			}

			_payloadSender.QueueSpan(this);

			_currentExecutionSegmentsContainer.CurrentSpan = _parentSpan;
		}

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null)
			=> ExecutionSegmentCommon.CaptureException(
				exception,
				_logger,
				_payloadSender,
				this,
				_configurationReader,
				_enclosingTransaction,
				culprit,
				isHandled,
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

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null)
			=> ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_payloadSender,
				_logger,
				this,
				_configurationReader,
				_enclosingTransaction,
				parentId
			);
	}
}
