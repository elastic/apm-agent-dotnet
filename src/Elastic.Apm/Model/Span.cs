// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal class Span : ExecutionSegment, ISpan
	{
		private readonly Lazy<SpanContext> _context = new Lazy<SpanContext>();
		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;
		private readonly Transaction _enclosingTransaction;

		private readonly bool _isDropped;
		private readonly Span _parentSpan;
		private readonly IPayloadSender _payloadSender;

		// // This constructor is meant for deserialization
		// [JsonConstructor]
		// private Span(double duration, string id, string name, string parentId)
		// {
		// 	Duration = duration;
		// 	Id = id;
		// 	Name = name;
		// 	ParentId = parentId;
		// }

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
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None
		) : base(name, logger, enclosingTransaction.ConfigSnapshot)
		{
			InstrumentationFlag = instrumentationFlag;
			Id = RandomGenerator.GenerateRandomBytesAsString(new byte[8]);

			_payloadSender = payloadSender;
			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;
			_parentSpan = parentSpan;
			_enclosingTransaction = enclosingTransaction;
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
					enclosingTransaction.SpanCount.IncrementStarted();
			}

			_currentExecutionSegmentsContainer.CurrentSpan = this;

			Logger.Trace()
				?.Log("New Span instance created: {Span}. Start time: {Time} (as timestamp: {Timestamp}). Parent span: {Span}",
					this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, _parentSpan);

			if (_parentSpan != null) _parentSpan.OnChildStart(Timestamp);
			else _enclosingTransaction.OnChildStart(Timestamp);
		}

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Action { get; set; }

		/// <summary>
		/// Any other arbitrary data captured by the agent, optionally provided by the user.
		/// <seealso cref="ShouldSerializeContext" />
		/// </summary>
		public SpanContext Context => _context.Value;

		internal InstrumentationFlag InstrumentationFlag { get; }

		[JsonIgnore]
		public override bool IsSampled => _enclosingTransaction.IsSampled;

		[JsonIgnore]
		public override Dictionary<string, string> Labels => Context.Labels;

		[JsonIgnore]
		public override DistributedTracingData OutgoingDistributedTracingData => new DistributedTracingData(
			TraceId,
			// When transaction is not sampled then outgoing distributed tracing data should have transaction ID for parent-id part
			// and not span ID as it does for sampled case.
			ShouldBeSentToApmServer ? Id : TransactionId,
			IsSampled);

		protected override string SegmentName => "Span";

		[JsonIgnore]
		internal bool ShouldBeSentToApmServer => IsSampled && !_isDropped;

		[JsonProperty("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Subtype { get; set; }

		//public decimal Start { get; set; }

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

		protected override Span CreateSpan(string name, string type, InstrumentationFlag instrumentationFlag)
			=> new Span(name, type, Id, TraceId, _enclosingTransaction, _payloadSender, Logger, _currentExecutionSegmentsContainer, this,
				instrumentationFlag);

		protected override void InternalEnd(bool isFirstEndCall, long endTimestamp)
		{
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
						StackTrace = StacktraceHelper.GenerateApmStackTrace(new StackTrace(true).GetFrames(), Logger,
							ConfigSnapshot, $"Span `{Name}'");
					}
				}

				_payloadSender.QueueSpan(this);
			}

			if (isFirstEndCall) _currentExecutionSegmentsContainer.CurrentSpan = _parentSpan;

			if (_parentSpan != null) _parentSpan.OnChildEnd(endTimestamp);
			else _enclosingTransaction.OnChildEnd(endTimestamp);
		}

		public override void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null)
			=> ExecutionSegmentCommon.CaptureException(
				exception,
				Logger,
				_payloadSender,
				this,
				ConfigSnapshot,
				_enclosingTransaction,
				culprit,
				isHandled,
				parentId ?? (ShouldBeSentToApmServer ? null : _enclosingTransaction.Id)
			);

		public override void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null)
			=> ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_payloadSender,
				Logger,
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
				return UrlUtils.ExtractDestination(Context.Http.OriginalUrl ?? new Uri(Context.Http.Url), Logger);
			}
			catch (Exception ex)
			{
				Logger.Trace()
					?.LogException(ex, "Failed to deduce destination info from Context.Http."
						+ " Original URL: {OriginalUrl}. Context.Http.Url: {Context.Http.Url}."
						, Context.Http.OriginalUrl, Context.Http.Url);
				return null;
			}
		}
	}
}
