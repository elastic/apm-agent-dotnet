// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;

namespace Elastic.Apm.Model
{
	/// <inheritdoc />
	internal class Span : ISpan
	{
		private readonly IApmServerInfo _apmServerInfo;

		private readonly ChildDurationTimer _childDurationTimer = new();
		private readonly Lazy<SpanContext> _context = new();
		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;
		private readonly Transaction _enclosingTransaction;

		private readonly bool _isDropped;
		private readonly IApmLogger _logger;
		private readonly Span _parentSpan;
		private readonly IPayloadSender _payloadSender;
		private readonly bool _restoreCurrentSpanOnEnd;

		private Span _compressionBuffer;

		// Indicates if the context was already propagated outside the span
		// This typically means that this span was already used for distributed tracing and potentially there is a span outside of the process
		// which points to this span.
		private bool _hasPropagatedContext;

		private bool Discardable => IsExitSpan && !_hasPropagatedContext && Outcome == Outcome.Success && Configuration.SpanCompressionEnabled;

		[JsonConstructor]
		// ReSharper disable once UnusedMember.Local - this is meant for deserialization
		private Span(double duration, string id, string name, string parentId)
		{
			Duration = duration;
			Id = id;
			Name = name;
			ParentId = parentId;
		}

		public Span(string name,
			string type,
			string parentId,
			string traceId,
			Transaction enclosingTransaction,
			IPayloadSender payloadSender,
			IApmLogger logger,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			IApmServerInfo apmServerInfo,
			Span parentSpan = null,
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None,
			bool captureStackTraceOnStart = false,
			long? timestamp = null,
			bool isExitSpan = false,
			string id = null,
			IEnumerable<SpanLink> links = null,
			Activity current = null,
			bool makeCurrent = true
		)
		{
			_logger = logger?.Scoped(nameof(Span));
			_payloadSender = payloadSender;
			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;
			_parentSpan = parentSpan;
			_enclosingTransaction = enclosingTransaction;
			_apmServerInfo = apmServerInfo;
			_restoreCurrentSpanOnEnd = makeCurrent;

			InstrumentationFlag = instrumentationFlag;
			Timestamp = timestamp ?? TimeUtils.TimestampNow();
			Id = id ?? ActivitySpanId.CreateRandom().ToString();
			IsExitSpan = isExitSpan;
			Name = name;
			Type = type;
			Links = links;

			_parentSpan?._childDurationTimer.OnChildStart(Timestamp);

			ParentId = parentId;
			TraceId = traceId;

			if (IsSampled)
			{
				SampleRate = enclosingTransaction.SampleRate;
				// Started and dropped spans should be counted only for sampled transactions
				if (enclosingTransaction.SpanCount.IncrementTotal() > Configuration.TransactionMaxSpans
					&& Configuration.TransactionMaxSpans >= 0)
				{
					_isDropped = true;
					enclosingTransaction.SpanCount.IncrementDropped();
				}
				else
				{
					enclosingTransaction.SpanCount.IncrementStarted();

					// In some cases capturing the stacktrace in End() results in a stack trace which is not very useful.
					// In such cases we capture the stacktrace on span start.
					// These are typically async calls - e.g. capturing stacktrace for outgoing HTTP requests in the
					// System.Net.Http.HttpRequestOut.Stop
					// diagnostic source event produces a stack trace that does not contain the caller method in user code - therefore we
					// capture the stacktrace in .Start
					if (captureStackTraceOnStart && IsCaptureStackTraceOnStartEnabled())
						RawStackTrace = new StackTrace(true);
				}
			}
			else
				SampleRate = 0;

			CheckAndCaptureBaggage();

			if (makeCurrent)
				_currentExecutionSegmentsContainer.CurrentSpan = this;

			var formattedTimestamp = _logger.IsEnabled(LogLevel.Trace) ? TimeUtils.FormatTimestampForLog(Timestamp) : string.Empty;

			_logger?.Trace()?.Log("New Span instance created: {Span}. Start time: {Time} (as timestamp: {Timestamp}). Parent span: {Span}",
				this, formattedTimestamp, Timestamp, _parentSpan);
		}

		private void CheckAndCaptureBaggage()
		{
			if (Activity.Current == null || !Activity.Current.Baggage.Any())
				return;

			foreach (var baggage in Activity.Current.Baggage)
			{
				if (!WildcardMatcher.IsAnyMatch(Configuration.BaggageToAttach, baggage.Key))
					continue;

				Otel ??= new OTel { Attributes = [] };

				var newKey = $"baggage.{baggage.Key}";
				Otel.Attributes[newKey] = baggage.Value;
			}
		}

		// Disable obsolete-warning due to Configuration.SpanFramesMinDurationInMilliseconds access.
#pragma warning disable CS0618
		// If the legacy setting (span_frames_min_duration) is present but the new
		// setting (span_stack_trace_min_duration) is not (or has a default value), the legacy setting dominates.
		private bool UseLegacyCaptureStackTraceSetting()
		{
			// If the legacy setting (span_frames_min_duration) is present but the new
			// setting (span_stack_trace_min_duration) is not (or has a default value), the legacy setting dominates.
			const double tolerance = 0.00001;
			return Math.Abs(Configuration.SpanFramesMinDurationInMilliseconds -
							ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds) > tolerance &&
				   Math.Abs(Configuration.SpanStackTraceMinDurationInMilliseconds -
							ConfigConsts.DefaultValues.SpanStackTraceMinDurationInMilliseconds) < tolerance;
		}

		internal bool IsCaptureStackTraceOnStartEnabled()
		{
			if (Configuration.StackTraceLimit != 0)
			{
				if (UseLegacyCaptureStackTraceSetting())
					return Configuration.SpanFramesMinDurationInMilliseconds != 0;

				return Configuration.SpanStackTraceMinDurationInMilliseconds >= 0;
			}
			return false;
		}

		internal bool IsCaptureStackTraceOnEndEnabled()
		{
			if (Configuration.StackTraceLimit != 0 && RawStackTrace == null)
			{
				return DurationRequiresCaptureStackTrace();
			}
			return false;
		}

		private bool DurationRequiresCaptureStackTrace()
		{
			if (UseLegacyCaptureStackTraceSetting())
			{
				return Configuration.SpanFramesMinDurationInMilliseconds != 0 &&
					(Duration >= Configuration.SpanFramesMinDurationInMilliseconds ||
						Configuration.SpanFramesMinDurationInMilliseconds < 0);
			}

			return Configuration.SpanStackTraceMinDurationInMilliseconds >= 0 &&
					Duration >= Configuration.SpanStackTraceMinDurationInMilliseconds;
		}
#pragma warning restore CS0618

		private int _ended;

		private bool IsEnded => Volatile.Read(ref _ended) != 0;

		/// <summary>
		/// In general if there is an error on the span, the outcome will be <code>Outcome.Failure</code>, otherwise it'll be
		/// <code>Outcome.Success</code>.
		/// There are some exceptions to this (see spec:
		/// https://github.com/elastic/apm/blob/main/specs/agents/tracing-spans.md#span-outcome) when it can be
		/// <code>Outcome.Unknown</code>.
		/// Use <see cref="_outcomeChangedThroughApi" /> to check if it was specifically set to <code>Outcome.Unknown</code>, or if
		/// it's just the default value.
		/// </summary>
		internal Outcome _outcome;

		private bool _outcomeChangedThroughApi;

		[MaxLength]
		public string Action { get; set; }

		/// <summary>
		/// Stores Context.Destination.Service.Resource and Contest.Service.Target on the top level.
		/// With this field, we can set Target.Name, Target.Type, and Resource for dropped spans without instantiating Context.
		/// Only set for dropped spans.
		/// </summary>
		[JsonIgnore]
		internal DroppedSpanStatCacheStruct? DroppedSpanStatCache { get; set; }

		[JsonIgnore]
		internal IConfiguration Configuration => _enclosingTransaction.Configuration;

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

		[MaxLength]
		public string Id { get; set; }

		internal InstrumentationFlag InstrumentationFlag { get; set; }

		[JsonIgnore]
		public bool IsExitSpan { get; }

		[JsonIgnore]
		public bool IsSampled => _enclosingTransaction.IsSampled;

		[JsonIgnore]
		[Obsolete(
			"Instead of this dictionary, use the `SetLabel` method which supports more types than just string. This property will be removed in a future release.")]
		public Dictionary<string, string> Labels => Context.Labels;

		[MaxLength]
		public string Name { get; set; }

		/// <summary>
		/// The outcome of the span: success, failure, or unknown.
		/// Outcome may be one of a limited set of permitted values describing the success or failure of the span.
		/// This field can be used for calculating error rates for outgoing requests.
		/// </summary>
		public Outcome Outcome
		{
			get => _outcome;
			set
			{
				_outcomeChangedThroughApi = true;
				_outcome = value;
			}
		}

		[JsonIgnore]
		public DistributedTracingData OutgoingDistributedTracingData
		{
			get
			{
				_hasPropagatedContext = true;
				return new(
					TraceId,
					// When transaction is not sampled then outgoing distributed tracing data should have transaction ID for parent-id part
					// and not span ID as it does for sampled case.
					ShouldBeSentToApmServer ? Id : TransactionId,
					IsSampled,
					_enclosingTransaction._traceState);
			}
		}

		[MaxLength]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		/// <summary>
		/// This holds the raw stack trace that was captured when the span either started or ended (depending on the parameter
		/// passed to the .ctor)
		/// This will be turned into an elastic stack trace and sent to APM Server in the <see cref="StackTrace" /> property
		/// </summary>
		internal StackTrace RawStackTrace;

		/// <summary>
		/// Links holds links to other spans, potentially in other traces.
		/// </summary>
		public IEnumerable<SpanLink> Links { get; private set; }

		public Composite Composite { get; set; }

		/// <summary>
		/// Captures the sample rate of the agent when this span was created.
		/// </summary>
		[JsonProperty("sample_rate")]
		internal double? SampleRate { get; }

		private double SelfDuration => Duration.HasValue ? Duration.Value - _childDurationTimer.Duration : 0;

		[JsonIgnore]
		internal bool ShouldBeSentToApmServer => IsSampled && !_isDropped;

		[JsonProperty("stacktrace")]
		public List<CapturedStackFrame> StackTrace { get; set; }

		[MaxLength]
		public string Subtype { get; set; }

		/// <summary>
		/// Recorded time of the event, UTC based and formatted as microseconds since Unix epoch
		/// </summary>
		public long Timestamp { get; internal set; }

		[MaxLength]
		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		[MaxLength]
		[JsonProperty("transaction_id")]
		public string TransactionId => _enclosingTransaction.Id;

		internal Transaction EnclosingTransaction => _enclosingTransaction;

		[MaxLength]
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
			{ nameof(Outcome), Outcome },
			{ nameof(IsSampled), IsSampled },
			{ nameof(Duration), Duration }
		}.ToString();

		public bool TryGetLabel<T>(string key, out T value)
		{
			if (Context.InternalLabels.Value.InnerDictionary.TryGetValue(key, out var label))
			{
				if (label?.Value is T t)
				{
					value = t;
					return true;
				}
			}

			value = default;
			return false;
		}

		public OTel Otel { get; set; }

		public ISpan StartSpan(string name, string type, string subType = null, string action = null, bool isExitSpan = false,
			IEnumerable<SpanLink> links = null
		)
		{
			if (Configuration.Enabled && Configuration.Recording)
				return StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links);

			return new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId);
		}

		internal Span StartSpanInternal(string name, string type, string subType = null, string action = null,
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None, bool captureStackTraceOnStart = false, long? timestamp = null,
			string id = null, bool isExitSpan = false, IEnumerable<SpanLink> links = null, Activity current = null, bool makeCurrent = true
		)
		{
			var span = new Span(name, type, Id, TraceId, _enclosingTransaction, _payloadSender, _logger, _currentExecutionSegmentsContainer,
				_apmServerInfo, this, instrumentationFlag, captureStackTraceOnStart, timestamp, isExitSpan, id, links, current: current,
				makeCurrent: makeCurrent);

			if (!string.IsNullOrEmpty(subType))
				span.Subtype = subType;

			if (!string.IsNullOrEmpty(action))
				span.Action = action;

			_logger?.Trace()?.Log("Starting {SpanDetails}", span.ToString());

			return span;
		}

		/// <summary>
		/// When the transaction has ended and before being queued to send to APM server
		/// </summary>
		public event EventHandler Ended;

		public void End()
		{
			if (Interlocked.CompareExchange(ref _ended, 1, 0) != 0)
				return;

			// If the outcome is still unknown and it was not specifically set to unknown, then it's success
			if (Outcome == Outcome.Unknown && !_outcomeChangedThroughApi)
				Outcome = Outcome.Success;

			var formattedTimestamp = _logger.IsEnabled(LogLevel.Trace) ? TimeUtils.FormatTimestampForLog(Timestamp) : string.Empty;

			if (Duration.HasValue)
			{
				_logger?.Trace()?.Log("Ended {Span} (with Duration already set)." +
						" Start time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, formattedTimestamp, Timestamp, Duration);

				if (_parentSpan != null)
					_parentSpan._childDurationTimer.OnChildEnd((long)(Timestamp + Duration.Value * 1000));
				else
					_enclosingTransaction.ChildDurationTimer.OnChildEnd((long)(Timestamp + Duration.Value * 1000));

				_childDurationTimer.OnSpanEnd((long)(Timestamp + Duration.Value * 1000));
			}
			else
			{
				var endTimestamp = TimeUtils.TimestampNow();
				Duration = TimeUtils.DurationBetweenTimestamps(Timestamp, endTimestamp);

				if (_parentSpan != null)
					_parentSpan._childDurationTimer.OnChildEnd(endTimestamp);
				else
					_enclosingTransaction.ChildDurationTimer.OnChildEnd(endTimestamp);

				_childDurationTimer.OnSpanEnd(endTimestamp);

				_logger?.Trace()?.Log("Ended {Span}. Start time: {Time} (as timestamp: {Timestamp})," +
						" End time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, formattedTimestamp, Timestamp,
						TimeUtils.FormatTimestampForLog(endTimestamp), endTimestamp, Duration);
			}

			var handler = Ended;
			handler?.Invoke(this, EventArgs.Empty);
			Ended = null;

			if (_enclosingTransaction.SpanTimings.ContainsKey(new SpanTimerKey(Type, Subtype)))
				_enclosingTransaction.SpanTimings[new SpanTimerKey(Type, Subtype)].IncrementTimer(SelfDuration);
			else
				_enclosingTransaction.SpanTimings.TryAdd(new SpanTimerKey(Type, Subtype), new SpanTimer(SelfDuration));

			try
			{
				DeduceServiceTarget();
			}
			catch (Exception e)
			{
				_logger?.Warning()?.LogException(e, "Failed deducing destination fields for span.");
			}

			if (_isDropped && _context.IsValueCreated)
			{
				_enclosingTransaction.UpdateDroppedSpanStats(Context?.Service?.Target?.Type, Context?.Service?.Target?.Name,
					Context?.Destination?.Service?.Resource, _outcome, Duration!.Value);
			}
			else if (_isDropped && !_context.IsValueCreated && DroppedSpanStatCache.HasValue)
			{
				_enclosingTransaction.UpdateDroppedSpanStats(DroppedSpanStatCache.Value.Target.Type, DroppedSpanStatCache.Value.Target.Name,
					DroppedSpanStatCache.Value.DestinationServiceResource, _outcome, Duration!.Value);
			}

			if (ShouldBeSentToApmServer)
			{
				// If we recorded the stack trace on start, but the duration of the span does not require
				// inclusion of the stack trace, remove it.
				if (RawStackTrace is not null && !DurationRequiresCaptureStackTrace())
					RawStackTrace = null;

				// Spans are sent only for sampled transactions so it's only worth capturing stack trace for sampled spans
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (IsCaptureStackTraceOnEndEnabled())
					RawStackTrace = new StackTrace(true);

				var buffered = _parentSpan?._compressionBuffer ?? _enclosingTransaction.CompressionBuffer;

				if (Configuration.SpanCompressionEnabled && _apmServerInfo?.Version >= new ElasticVersion(8, 0, 0, string.Empty))
				{
					if (!IsCompressionEligible() || _parentSpan is { IsEnded: true })
					{
						if (buffered != null)
						{
							QueueSpan(buffered);
							if (_parentSpan != null)
								_parentSpan._compressionBuffer = null;
							_enclosingTransaction.CompressionBuffer = null;
						}

						//If this is a span which has buffered children, we send the composite.
						if (_compressionBuffer != null)
							QueueSpan(_compressionBuffer);

						QueueSpan(this);
						RestoreCurrentSpan();
						return;
					}
					if (buffered == null)
					{
						SetThisToParentsBuffer();
						RestoreCurrentSpan();
						return;
					}

					if (!buffered.TryToCompress(this))
					{
						QueueSpan(buffered);
						SetThisToParentsBuffer();
						RestoreCurrentSpan();
					}
				}
				else
					QueueSpan(this);
			}

			RestoreCurrentSpan();

			void QueueSpan(Span span)
			{
				if (span.Composite != null)
				{
					var endTimestamp = TimeUtils.TimestampNow();
					span.Duration = TimeUtils.DurationBetweenTimestamps(span.Timestamp, endTimestamp);
				}

				if (span.Discardable)
				{
					if (span.Composite != null && span.Duration < span.Configuration.ExitSpanMinDuration)
					{
						switch (_context.IsValueCreated)
						{
							case true:
								_enclosingTransaction.UpdateDroppedSpanStats(Context?.Service?.Target?.Type, Context?.Service?.Target?.Name,
									Context?.Destination?.Service?.Resource, _outcome, Duration!.Value);
								break;
							case false when DroppedSpanStatCache.HasValue:
								_enclosingTransaction.UpdateDroppedSpanStats(DroppedSpanStatCache.Value.Target.Type,
									DroppedSpanStatCache.Value.Target.Name,
									DroppedSpanStatCache.Value.DestinationServiceResource, _outcome, Duration!.Value);
								break;
						}
						_logger?.Trace()?.Log("Dropping fast exit span on composite span. Composite duration: {duration}", span.Composite.Sum);
						return;
					}
					if (span.Duration < span.Configuration.ExitSpanMinDuration)
					{
						switch (_context.IsValueCreated)
						{
							case true:
								_enclosingTransaction.UpdateDroppedSpanStats(Context?.Service?.Target?.Type, Context?.Service?.Target?.Name,
									Context?.Destination?.Service?.Resource, _outcome, Duration!.Value);
								break;
							case false when DroppedSpanStatCache.HasValue:
								_enclosingTransaction.UpdateDroppedSpanStats(DroppedSpanStatCache.Value.Target.Type,
									DroppedSpanStatCache.Value.Target.Name,
									DroppedSpanStatCache.Value.DestinationServiceResource, _outcome, Duration!.Value);
								break;
						}
						_logger?.Trace()?.Log("Dropping fast exit span. Duration: {duration}", span.Duration);
						return;
					}
				}

				_payloadSender.QueueSpan(span);
			}
		}

		private void RestoreCurrentSpan()
		{
			if (_restoreCurrentSpanOnEnd && ReferenceEquals(_currentExecutionSegmentsContainer.CurrentSpan, this))
				_currentExecutionSegmentsContainer.CurrentSpan = _parentSpan;
		}

		/// <summary>
		/// Drops this span without reporting it. Releases child-duration bookkeeping on the parent so an orphaned
		/// in-flight span cannot leak <c>activeChildren</c> or inflate parent child-duration / deflate self-time.
		/// Mutually exclusive with <see cref="End"/> via a shared completion gate.
		/// </summary>
		internal void Abandon()
		{
			if (Interlocked.CompareExchange(ref _ended, 1, 0) != 0)
				return;

			_logger?.Trace()?.Log("Abandoning {Span} (not reported)", this);

			if (_parentSpan != null)
				_parentSpan._childDurationTimer.OnChildAbandoned(Timestamp);
			else
				_enclosingTransaction.ChildDurationTimer.OnChildAbandoned(Timestamp);

			_childDurationTimer.OnSpanEnd(TimeUtils.TimestampNow());
			Ended = null;
			RestoreCurrentSpan();
		}

		private bool TryToCompress(Span sibling)
		{
			var isAlreadyComposite = Composite != null;
			var canBeCompressed = isAlreadyComposite ? TryToCompressComposite(sibling) : TryToCompressRegular(sibling);
			if (!canBeCompressed)
				return false;


			if (!isAlreadyComposite)
			{
				Composite ??= new Composite();
				Composite.Count = 1;
				Composite.Sum = Duration!.Value;
			}

			Composite.Count++;
			Composite.Sum += sibling.Duration!.Value;
			return true;
		}

		private bool IsSameKind(Span other) => Type == other.Type
			&& Subtype == other.Subtype
			&& _context.IsValueCreated && other._context.IsValueCreated
			&& Context?.Service?.Target == other.Context?.Service?.Target;

		private bool TryToCompressRegular(Span sibling)
		{
			if (!IsSameKind(sibling))
				return false;

			if (Name == sibling.Name)
			{
				if (Duration <= Configuration.SpanCompressionExactMatchMaxDuration
					&& sibling.Duration <= Configuration.SpanCompressionExactMatchMaxDuration)
				{
					Composite ??= new Composite();
					Composite.CompressionStrategy = "exact_match";
					return true;
				}

				return false;
			}

			if (Duration <= Configuration.SpanCompressionSameKindMaxDuration && sibling.Duration <= Configuration.SpanCompressionSameKindMaxDuration)
			{
				Composite ??= new Composite();
				Composite.CompressionStrategy = "same_kind";
				if (_context.IsValueCreated && Context?.Service?.Target != null)
				{
					Name = "Calls to " + Context.Service.Target.ToDestinationServiceResource();
				}

				return true;
			}

			return false;
		}

		private bool TryToCompressComposite(Span sibling)
		{
			switch (Composite.CompressionStrategy)
			{
				case "exact_match":
					return IsSameKind(sibling) && Name == sibling.Name && sibling.Duration <= Configuration.SpanCompressionExactMatchMaxDuration;

				case "same_kind":
					return IsSameKind(sibling) && sibling.Duration <= Configuration.SpanCompressionSameKindMaxDuration;
			}

			return false;
		}

		internal void InsertSpanLinkInternal(IEnumerable<SpanLink> links)
		{
			var spanLinks = links as SpanLink[] ?? links.ToArray();
			if (Links == null || !Links.Any())
				Links = spanLinks;

			var newList = new List<SpanLink>(Links);
			newList.AddRange(spanLinks);
			Links = new List<SpanLink>(newList);
		}

		private void SetThisToParentsBuffer()
		{
			if (_parentSpan != null)
				_parentSpan._compressionBuffer = this;
			else
				_enclosingTransaction.CompressionBuffer = this;
		}

		public bool IsCompressionEligible() => IsExitSpan && !_hasPropagatedContext && Outcome is Outcome.Success or Outcome.Unknown;

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null,
			Dictionary<string, Label> labels = null
		)
			=> ExecutionSegmentCommon.CaptureException(
				exception,
				_logger,
				_payloadSender,
				this,
				Configuration,
				_enclosingTransaction,
				_apmServerInfo,
				culprit,
				isHandled,
				parentId ?? (ShouldBeSentToApmServer ? null : _enclosingTransaction.Id),
				labels
			);

		public void CaptureSpan(string name, string type, Action<ISpan> capturedAction, string subType = null, string action = null,
			bool isExitSpan = false, IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links),
				capturedAction);

		public void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null, bool isExitSpan = false,
			IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links),
				capturedAction);

		public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null, bool isExitSpan = false,
			IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links), func);

		public T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null, bool isExitSpan = false,
			IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links), func);

		public Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null, bool isExitSpan = false,
			IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links), func);

		public Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null,
			bool isExitSpan = false, IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null,
			bool isExitSpan = false, IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null,
			bool isExitSpan = false, IEnumerable<SpanLink> links = null
		)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpanInternal(name, type, subType, action, isExitSpan: isExitSpan, links: links), func);

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null, Dictionary<string, Label> labels = null)
			=> ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_payloadSender,
				_logger,
				this,
				Configuration,
				_enclosingTransaction,
				_apmServerInfo,
				parentId ?? (ShouldBeSentToApmServer ? null : _enclosingTransaction.Id),
				labels
			);

		private void DeduceServiceTarget()
		{
			if (!IsExitSpan)
				return;

			// In order to avoid the creation of Context, set target and resource
			// on the top level DroppedSpanStatCache and return.
			if (!_context.IsValueCreated && !IsExitSpan && _isDropped)
			{
				if (DroppedSpanStatCache == null)
				{
					var type = !string.IsNullOrEmpty(Subtype) ? Subtype : Type;
					DroppedSpanStatCache = new DroppedSpanStatCacheStruct(Target.TargetWithType(type), type);
					return;
				}
			}

			if (Context.Http != null)
			{
				var destination = DeduceHttpDestination();
				if (destination == null)
					// In case of invalid destination just return
					return;

				CopyMissingProperties(destination);
			}

			FillDestinationService();

			// Fills Context.Destination.Service
			void FillDestinationService()
			{
				if (!IsExitSpan)
					return;

				if (_context.IsValueCreated && !string.IsNullOrEmpty(_context.Value.Destination?.Service?.Resource))
					return;

				if (_context.IsValueCreated && _context.Value.Service?.Target != null)
				{
					// Nothing to do here.
					// If "Service.Target" is already set, the inference mechanism should not override it.
					// We need to make sure though to set "Destination.Service.Resource" before exiting.
				}
				else
				{
					var type = !string.IsNullOrEmpty(Subtype) ? Subtype : Type;

					if (Context.Db != null)
					{
						Context.Service = Context.Db.Instance != null
							? new SpanService(new Target(type, Context.Db.Instance))
							: new SpanService(Target.TargetWithType(type));
					}
					else if (Context.Message != null)
					{
						Context.Service = !string.IsNullOrEmpty(Context.Message.Queue?.Name)
							? new SpanService(new Target(type, Context.Message.Queue.Name))
							: new SpanService(Target.TargetWithType(type));
					}
					else if (Context.Http?.Url != null)
					{
						if (!string.IsNullOrEmpty(_context?.Value?.Http?.Url))
						{
							try
							{
								var uri = Context.Http.OriginalUrl ?? new Uri(Context.Http.Url);
								Context.Service =
									new SpanService(new Target(type, UrlUtils.ExtractService(uri, this), true));
							}
							catch
							{
								Context.Service = new SpanService(Target.TargetWithType(type));
							}
						}
						else
							Context.Service = new SpanService(Target.TargetWithType(type));
					}
					else
						Context.Service = new SpanService(Target.TargetWithType(type));
				}

				Context.Destination ??= new Destination();
				Context.Destination.Service = new Destination.DestinationService
				{
					Resource = Context.Service.Target.ToDestinationServiceResource()
				};
			}

			void CopyMissingProperties(Destination src)
			{
				if (src == null)
					return;

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
				_logger?.Trace()?.LogException(ex, "Failed to deduce destination info from Context.Http."
						+ " Original URL: {OriginalUrl}. Context.Http.Url: {Context.Http.Url}."
						, Context.Http.OriginalUrl, Context.Http.Url);
				return null;
			}
		}

		public void SetLabel(string key, string value)
			=> Context.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, bool value)
			=> Context.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, double value)
			=> Context.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, int value)
			=> Context.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, long value)
			=> Context.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, decimal value)
			=> Context.InternalLabels.Value.InnerDictionary[key] = value;

		public void CaptureErrorLog(ErrorLog errorLog, string parentId = null, Exception exception = null, Dictionary<string, Label> labels = null)
			=> ExecutionSegmentCommon.CaptureErrorLog(
				errorLog,
				_payloadSender,
				_logger,
				this,
				Configuration,
				_enclosingTransaction,
				parentId ?? (ShouldBeSentToApmServer ? null : _enclosingTransaction.Id),
				_apmServerInfo,
				exception,
				labels
			);

		internal struct DroppedSpanStatCacheStruct
		{
			public DroppedSpanStatCacheStruct(Target target, string destinationServiceResource) =>
				(Target, DestinationServiceResource) = (target, destinationServiceResource);

			internal Target Target { get; }
			internal string DestinationServiceResource { get; }
		}
	}

	internal class SpanTimer
	{
		public SpanTimer(double duration)
		{
			TotalDuration = duration;
			Count = 1;
		}

		public int Count { get; set; }

		public double TotalDuration { get; set; }

		public void IncrementTimer(double duration)
		{
			Count++;
			TotalDuration += duration;
		}
	}

	/// <summary>
	/// Composite holds details on a group of spans represented by a single one.
	/// </summary>
	internal class Composite
	{
		/// <summary>
		/// A string value indicating which compression strategy was used. The valid values are `exact_match` and `same_kind`
		/// </summary>
		[JsonProperty("compression_strategy")]
		public string CompressionStrategy { get; set; }

		/// <summary>
		/// Count is the number of compressed spans the composite span represents. The minimum count is 2, as a composite span represents at least two spans.
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// Sum is the durations of all compressed spans this composite span represents in milliseconds.
		/// </summary>
		public double Sum { get; set; }
	}

	internal class ChildDurationTimer
	{
		private readonly object _lock = new();
		private readonly List<long> _activeStarts = [];
		private readonly List<(long Start, long End)> _waveIntervals = new();
		private double _duration;
		private bool _isEnded;

		public double Duration
		{
			get
			{
				lock (_lock)
					return _duration;
			}
		}

		internal int ActiveChildren
		{
			get
			{
				lock (_lock)
					return _activeStarts.Count;
			}
		}

		/// <summary>
		/// Registers a direct child starting at <paramref name="startTimestamp"/>.
		/// </summary>
		public void OnChildStart(long startTimestamp)
		{
			lock (_lock)
			{
				if (_isEnded)
					return;

				_activeStarts.Add(startTimestamp);
			}
		}

		/// <summary>
		/// Registers a direct child ending at <paramref name="endTimestamp"/> and, when no other direct children
		/// remain, adds the union of that wave's intervals to <see cref="Duration"/>.
		/// </summary>
		public void OnChildEnd(long endTimestamp)
		{
			lock (_lock)
			{
				if (_isEnded || _activeStarts.Count == 0)
					return;

				var start = TakeStartForEnd(endTimestamp);
				AddWaveInterval(start, Math.Max(endTimestamp, start));

				if (_activeStarts.Count == 0)
					FlushWave();
			}
		}

		/// <summary>
		/// Drops a direct child that will not be reported, identified by its start timestamp.
		/// Does not contribute duration for that child; any intervals already completed by siblings in the
		/// same wave are still flushed when the wave becomes empty.
		/// </summary>
		public void OnChildAbandoned(long startTimestamp)
		{
			lock (_lock)
			{
				if (_isEnded || _activeStarts.Count == 0)
					return;

				var index = _activeStarts.LastIndexOf(startTimestamp);
				if (index < 0)
					return;

				_activeStarts.RemoveAt(index);

				if (_activeStarts.Count == 0)
					FlushWave();
			}
		}

		/// <summary>
		/// Closes any still-open children at <paramref name="endTimestamp"/> and finalizes the timer.
		/// </summary>
		public void OnSpanEnd(long endTimestamp)
		{
			lock (_lock)
			{
				if (_isEnded)
					return;

				foreach (var start in _activeStarts)
					AddWaveInterval(start, Math.Max(endTimestamp, start));

				_activeStarts.Clear();
				FlushWave();
				_isEnded = true;
			}
		}

		/// <summary>
		/// Pairs an end with the latest still-open start at or before the end timestamp (nested/overlapping
		/// friendly). Falls back to the earliest open start when all starts are after the end.
		/// </summary>
		private long TakeStartForEnd(long endTimestamp)
		{
			var bestIndex = -1;
			for (var i = 0; i < _activeStarts.Count; i++)
			{
				var start = _activeStarts[i];
				if (start <= endTimestamp && (bestIndex < 0 || start > _activeStarts[bestIndex]))
					bestIndex = i;
			}

			if (bestIndex < 0)
			{
				bestIndex = 0;
				for (var i = 1; i < _activeStarts.Count; i++)
				{
					if (_activeStarts[i] < _activeStarts[bestIndex])
						bestIndex = i;
				}
			}

			var chosen = _activeStarts[bestIndex];
			_activeStarts.RemoveAt(bestIndex);
			return chosen;
		}

		private void AddWaveInterval(long start, long end)
		{
			if (end < start)
				end = start;
			_waveIntervals.Add((start, end));
		}

		private void FlushWave()
		{
			if (_waveIntervals.Count == 0)
				return;

			_duration += UnionDurationMilliseconds(_waveIntervals);
			_waveIntervals.Clear();
		}

		private static double UnionDurationMilliseconds(List<(long Start, long End)> intervals)
		{
			intervals.Sort(static (a, b) => a.Start.CompareTo(b.Start));

			var unionStart = intervals[0].Start;
			var unionEnd = intervals[0].End;
			double total = 0;

			for (var i = 1; i < intervals.Count; i++)
			{
				var (start, end) = intervals[i];
				if (start <= unionEnd)
					unionEnd = Math.Max(unionEnd, end);
				else
				{
					total += TimeUtils.DurationBetweenTimestamps(unionStart, unionEnd);
					unionStart = start;
					unionEnd = end;
				}
			}

			total += TimeUtils.DurationBetweenTimestamps(unionStart, unionEnd);
			return total;
		}
	}
}
