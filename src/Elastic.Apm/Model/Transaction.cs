// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Config;
using Elastic.Apm.DistributedTracing;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report;
using Elastic.Apm.ServerInfo;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Libraries.Newtonsoft.Json.Converters;
using Elastic.Apm.Libraries.Newtonsoft.Json.Serialization;

namespace Elastic.Apm.Model
{
	internal class Transaction : ITransaction
	{
		private static readonly string ApmTransactionActivityName = "ElasticApm.Transaction";
		private readonly IApmServerInfo _apmServerInfo;
		private readonly Lazy<Context> _context = new Lazy<Context>();
		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;
		private readonly IApmLogger _logger;
		private readonly IPayloadSender _sender;

		internal readonly TraceState _traceState;

		// This constructor is meant for serialization
		[JsonConstructor]
		private Transaction(Context context, string name, string type, double duration, long timestamp, string id, string traceId, string parentId,
			bool isSampled, string result, SpanCount spanCount
		)
		{
			_context = new Lazy<Context>(() => context);
			Name = name;
			Duration = duration;
			Timestamp = timestamp;
			Type = type;
			Id = id;
			TraceId = traceId;
			ParentId = parentId;
			IsSampled = isSampled;
			Result = result;
			SpanCount = spanCount;
		}

		// This constructor is used only by tests that don't care about sampling and distributed tracing
		internal Transaction(ApmAgent agent, string name, string type)
			: this(agent.Logger, name, type, new Sampler(1.0), null, agent.PayloadSender, agent.ConfigStore.CurrentSnapshot,
				agent.TracerInternal.CurrentExecutionSegmentsContainer, null)
		{ }

		/// <summary>
		/// Creates a new transaction
		/// </summary>
		/// <param name="logger">The logger which logs debug information during the transaction  creation process</param>
		/// <param name="name">The name of the transaction</param>
		/// <param name="type">The type of the transaction</param>
		/// <param name="sampler">The sampler implementation which makes the sampling decision</param>
		/// <param name="distributedTracingData">Distributed tracing data, in case this transaction is part of a distributed trace</param>
		/// <param name="sender">The IPayloadSender implementation which will record this transaction</param>
		/// <param name="configSnapshot">The current configuration snapshot which contains the up-do-date config setting values</param>
		/// <param name="currentExecutionSegmentsContainer" />
		/// The ExecutionSegmentsContainer which makes sure this transaction flows
		/// <param name="apmServerInfo">Component to fetch info about APM Server (e.g. APM Server version)</param>
		/// <param name="ignoreActivity">
		/// If set the transaction will ignore Activity.Current and it's trace id,
		/// otherwise the agent will try to keep ids in-sync across async work-flows
		/// </param>
		internal Transaction(
			IApmLogger logger,
			string name,
			string type,
			Sampler sampler,
			DistributedTracingData distributedTracingData,
			IPayloadSender sender,
			IConfigSnapshot configSnapshot,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer,
			IApmServerInfo apmServerInfo,
			bool ignoreActivity = false
		)
		{
			ConfigSnapshot = configSnapshot;
			Timestamp = TimeUtils.TimestampNow();

			_logger = logger?.Scoped(nameof(Transaction));
			_apmServerInfo = apmServerInfo;
			_sender = sender;
			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;

			Name = name;
			HasCustomName = false;
			Type = type;

			// For each new transaction, start an Activity if we're not ignoring them.
			// If Activity.Current is not null, the started activity will be a child activity,
			// so the traceid and tracestate of the parent will flow to it.
			if (!ignoreActivity)
				_activity = StartActivity();

			var isSamplingFromDistributedTracingData = false;
			if (distributedTracingData == null || configSnapshot.SuppressTraceContextHeaders)
			{
				// We consider a newly created transaction **without** explicitly passed distributed tracing data
				// to be a root transaction.
				// Ignore the created activity ActivityTraceFlags because it starts out without setting the IsSampled flag,
				// so relying on that would mean a transaction is never sampled.
				if (_activity != null)
				{
					// If an activity was created, reuse its id
					Id = _activity.SpanId.ToHexString();
					TraceId = _activity.TraceId.ToHexString();

					var idBytesFromActivity = new Span<byte>(new byte[16]);
					_activity.TraceId.CopyTo(idBytesFromActivity);

					// Read right most bits. From W3C TraceContext: "it is important for trace-id to carry "uniqueness" and "randomness"
					// in the right part of the trace-id..."
					idBytesFromActivity = idBytesFromActivity.Slice(8);

					_traceState = new TraceState();

					// If activity has a tracestate, populate the transaction tracestate with it.
					if (!string.IsNullOrEmpty(_activity.TraceStateString))
						_traceState.AddTextHeader(_activity.TraceStateString);

					IsSampled = sampler.DecideIfToSample(idBytesFromActivity.ToArray());

					// In the unlikely event that tracestate populated from activity contains an es vendor key, the tracestate
					// is mutated to set the sample rate defined by the sampler, because we consider a transaction without
					// explicitly passed distributedTracingData to be a **root** transaction. The end result
					// is that activity tracestate will be propagated, along with the sample rate defined by this transaction.
					if (IsSampled)
					{
						SampleRate = sampler.Rate;
						_traceState.SetSampleRate(sampler.Rate);
					}
					else
					{
						SampleRate = 0;
						_traceState.SetSampleRate(0);
					}

					// sync the activity tracestate with the tracestate of the transaction
					_activity.TraceStateString = _traceState.ToTextHeader();
				}
				else
				{
					// If no activity is created, create new random ids
					var idBytes = new byte[8];
					Id = RandomGenerator.GenerateRandomBytesAsString(idBytes);
					IsSampled = sampler.DecideIfToSample(idBytes);

					idBytes = new byte[16];
					TraceId = RandomGenerator.GenerateRandomBytesAsString(idBytes);

					if (IsSampled)
					{
						_traceState = new TraceState(sampler.Rate);
						SampleRate = sampler.Rate;
					}
					else
					{
						_traceState = new TraceState(0);
						SampleRate = 0;
					}
				}

				// ParentId could be also set here, but currently in the UI each trace must start with a transaction where the ParentId is null,
				// so to avoid https://github.com/elastic/apm-agent-dotnet/issues/883 we don't set it yet.
			}
			else
			{
				if (_activity != null)
				{
					Id = _activity.SpanId.ToHexString();

					// try to set the parent id and tracestate on the created activity, based on passed distributed tracing data.
					// This is so that the distributed tracing data will flow to any child activities
					try
					{
						_activity.SetParentId(
							ActivityTraceId.CreateFromString(distributedTracingData.TraceId.AsSpan()),
							ActivitySpanId.CreateFromString(distributedTracingData.ParentId.AsSpan()),
							distributedTracingData.FlagRecorded ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None);

						if (distributedTracingData.HasTraceState)
							_activity.TraceStateString = distributedTracingData.TraceState.ToTextHeader();
					}
					catch (Exception e)
					{
						_logger.Error()?.LogException(e, "Error setting trace context on created activity");
					}
				}
				else
				{
					var idBytes = new byte[8];
					Id = RandomGenerator.GenerateRandomBytesAsString(idBytes);
				}

				TraceId = distributedTracingData.TraceId;
				ParentId = distributedTracingData.ParentId;
				IsSampled = distributedTracingData.FlagRecorded;
				isSamplingFromDistributedTracingData = true;
				_traceState = distributedTracingData.TraceState;

				// If there is no tracestate or no valid "es" vendor entry with an "s" (sample rate) attribute, then the agent must
				// omit sample rate from non-root transactions and their spans.
				// See https://github.com/elastic/apm/blob/master/specs/agents/tracing-sampling.md#propagation
				if (_traceState?.SampleRate is null)
					SampleRate = null;
				else
					SampleRate = _traceState.SampleRate.Value;
			}

			// Also mark the sampling decision on the Activity
			if (IsSampled && _activity != null)
				_activity.ActivityTraceFlags |= ActivityTraceFlags.Recorded;

			SpanCount = new SpanCount();
			_currentExecutionSegmentsContainer.CurrentTransaction = this;

			if (isSamplingFromDistributedTracingData)
			{
				_logger.Trace()
					?.Log("New Transaction instance created: {Transaction}. " +
						"IsSampled ({IsSampled}) and SampleRate ({SampleRate}) is based on incoming distributed tracing data ({DistributedTracingData})." +
						" Start time: {Time} (as timestamp: {Timestamp})",
						this, IsSampled, SampleRate, distributedTracingData, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
			}
			else
			{
				_logger.Trace()
					?.Log("New Transaction instance created: {Transaction}. " +
						"IsSampled ({IsSampled}) is based on the given sampler ({Sampler})." +
						" Start time: {Time} (as timestamp: {Timestamp})",
						this, IsSampled, sampler, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
			}
		}

		private Activity StartActivity()
		{
			var activity = new Activity(ApmTransactionActivityName);
			activity.SetIdFormat(ActivityIdFormat.W3C);
			activity.Start();
			return activity;
		}

		/// <summary>
		/// The agent also starts an Activity when a transaction is started and stops it when the transaction ends.
		/// The TraceId of this activity is always the same as the TraceId of the transaction.
		/// With this, in case Activity.Current is null, the agent will set it and when the next Activity gets created it'll
		/// have this activity as its parent and the TraceId will flow to all Activity instances.
		/// </summary>
		private readonly Activity _activity;

		private bool _isEnded;

		private string _name;

		/// <summary>
		/// Holds configuration snapshot (which is immutable) that was current when this transaction started.
		/// We would like transaction data to be consistent and not to be affected by possible changes in agent's configuration
		/// between the start and the end of the transaction. That is why the way all the data is collected for the transaction
		/// and its spans is controlled by this configuration snapshot.
		/// </summary>
		[JsonIgnore]
		internal IConfigSnapshot ConfigSnapshot { get; }

		/// <summary>
		/// Any arbitrary contextual information regarding the event, captured by the agent, optionally provided by the user.
		/// <seealso cref="ShouldSerializeContext" />
		/// </summary>
		public Context Context => _context.Value;

		[JsonIgnore]
		public Dictionary<string, string> Custom => Context.Custom;

		/// <inheritdoc />
		/// <summary>
		/// The duration of the transaction.
		/// If it's not set (HasValue returns false) then the value
		/// is automatically calculated when <see cref="End" /> is called.
		/// </summary>
		/// <value>The duration.</value>
		public double? Duration { get; set; }

		/// <summary>
		/// If true, then the transaction name was modified by external code, and transaction name should not be changed
		/// or "fixed" automatically.
		/// </summary>
		[JsonIgnore]
		internal bool HasCustomName { get; private set; }

		[MaxLength]
		public string Id { get; }

		[JsonIgnore]
		internal bool IsContextCreated => _context.IsValueCreated;

		[JsonProperty("sampled")]
		public bool IsSampled { get; }

		[JsonIgnore]
		[Obsolete(
			"Instead of this dictionary, use the `SetLabel` method which supports more types than just string. This property will be removed in a future release.")]
		public Dictionary<string, string> Labels => Context.Labels;

		[MaxLength]
		public string Name
		{
			get => _name;
			set
			{
				HasCustomName = true;
				_name = value;
			}
		}

		/// <summary>
		/// The outcome of the transaction: success, failure, or unknown.
		/// This is similar to 'result', but has a limited set of permitted values describing the success or failure of the
		/// transaction from the service's perspective.
		/// This field can be used for calculating error rates for incoming requests.
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

		/// <summary>
		/// In general if there is an error on the span, the outcome will be <see cref="Outcome.Failure"/>, otherwise it'll be <see cref="Outcome.Success"/>.
		/// There are some exceptions to this (see spec: https://github.com/elastic/apm/blob/master/specs/agents/tracing-spans.md#span-outcome) when it can be <see cref="Outcome.Unknown"/>.
		/// Use <see cref="_outcomeChangedThroughApi"/> to check if it was specifically set to <see cref="Outcome.Unknown"/>, or if it's just the default value.
		/// </summary>
		internal Outcome _outcome;

		private bool _outcomeChangedThroughApi;

		[JsonIgnore]
		public DistributedTracingData OutgoingDistributedTracingData => new DistributedTracingData(TraceId, Id, IsSampled, _traceState);

		[MaxLength]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		/// <inheritdoc />
		/// <summary>
		/// A string describing the result of the transaction.
		/// This is typically the HTTP status code, or e.g. "success" for a background task.
		/// </summary>
		/// <value>The result.</value>
		[MaxLength]
		public string Result { get; set; }

		/// <summary>
		/// Captures the sample rate of the agent when this transaction was created.
		/// </summary>
		[JsonProperty("sample_rate")]
		internal double? SampleRate { get; }

		internal Service Service;


		[JsonProperty("span_count")]
		public SpanCount SpanCount { get; set; }

		/// <summary>
		/// Recorded time of the event, UTC based and formatted as microseconds since Unix epoch
		/// </summary>
		public long Timestamp { get; }

		[MaxLength]
		[JsonProperty("trace_id")]
		public string TraceId { get; }

		[MaxLength]
		public string Type { get; set; }

		/// <inheritdoc />
		public void SetService(string serviceName, string serviceVersion)
		{
			if (Context.Service == null)
				Context.Service = new Service(serviceName, serviceVersion);
			else
			{
				Context.Service.Name = serviceName;
				Context.Service.Version = serviceVersion;
			}
		}

		public string EnsureParentId()
		{
			if (!string.IsNullOrEmpty(ParentId))
				return ParentId;

			var idBytes = new byte[8];
			ParentId = RandomGenerator.GenerateRandomBytesAsString(idBytes);
			_logger?.Debug()?.Log("Setting ParentId to transaction, {transaction}", this);
			return ParentId;
		}

		/// <summary>
		/// Method to conditionally serialize <see cref="Context" /> because context should be serialized only when the transaction
		/// is sampled.
		/// See
		/// <a href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm">the relevant Json.NET Documentation</a>
		/// </summary>
		public bool ShouldSerializeContext() => IsSampled;

		public override string ToString() => new ToStringBuilder(nameof(Transaction))
		{
			{ nameof(Id), Id },
			{ nameof(TraceId), TraceId },
			{ nameof(ParentId), ParentId },
			{ nameof(Name), Name },
			{ nameof(Type), Type },
			{ nameof(Outcome), Outcome },
			{ nameof(IsSampled), IsSampled }
		}.ToString();

		/// <summary>
		/// When the transaction has ended and before being queued to send to APM server
		/// </summary>
		public event EventHandler Ended;

		public void End()
		{
			// If the outcome is still unknown and it was not specifically set to unknown, then it's success
			if (Outcome == Outcome.Unknown && !_outcomeChangedThroughApi)
				Outcome = Outcome.Success;

			if (Duration.HasValue)
			{
				_logger.Trace()
					?.Log("Ended {Transaction} (with Duration already set)." +
						" Start time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, Duration);
			}
			else
			{
				Assertion.IfEnabled?.That(!_isEnded,
					$"Transaction's Duration doesn't have value even though {nameof(End)} method was already called." +
					$" It contradicts the invariant enforced by {nameof(End)} method - Duration should have value when {nameof(End)} method exits" +
					$" and {nameof(_isEnded)} field is set to true only when {nameof(End)} method exits." +
					$" Context: this: {this}; {nameof(_isEnded)}: {_isEnded}");

				var endTimestamp = TimeUtils.TimestampNow();
				Duration = TimeUtils.DurationBetweenTimestamps(Timestamp, endTimestamp);
				_logger.Trace()
					?.Log("Ended {Transaction}. Start time: {Time} (as timestamp: {Timestamp})," +
						" End time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp,
						TimeUtils.FormatTimestampForLog(endTimestamp), endTimestamp, Duration);
			}

			_activity?.Stop();

			var isFirstEndCall = !_isEnded;
			_isEnded = true;
			if (!isFirstEndCall)
				return;

			var handler = Ended;
			handler?.Invoke(this, EventArgs.Empty);
			Ended = null;

			_sender.QueueTransaction(this);
			_currentExecutionSegmentsContainer.CurrentTransaction = null;
		}

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

		public ISpan StartSpan(string name, string type, string subType = null, string action = null)
		{
			if (ConfigSnapshot.Enabled && ConfigSnapshot.Recording)
				return StartSpanInternal(name, type, subType, action);

			return new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId);
		}

		internal Span StartSpanInternal(string name, string type, string subType = null, string action = null,
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None, bool captureStackTraceOnStart = false
		)
		{
			var retVal = new Span(name, type, Id, TraceId, this, _sender, _logger, _currentExecutionSegmentsContainer, _apmServerInfo,
				instrumentationFlag: instrumentationFlag, captureStackTraceOnStart: captureStackTraceOnStart);

			if (!string.IsNullOrEmpty(subType))
				retVal.Subtype = subType;

			if (!string.IsNullOrEmpty(action))
				retVal.Action = action;

			_logger.Trace()?.Log("Starting {SpanDetails}", retVal.ToString());
			return retVal;
		}

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null,
			Dictionary<string, Label> labels = null
		)
			=> ExecutionSegmentCommon.CaptureException(
				exception,
				_logger,
				_sender,
				this,
				ConfigSnapshot,
				this,
				_apmServerInfo,
				culprit,
				isHandled,
				parentId,
				labels
			);

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null, Dictionary<string, Label> labels = null)
			=> ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_sender,
				_logger,
				this,
				ConfigSnapshot,
				this,
				_apmServerInfo,
				parentId,
				labels
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

		internal static string StatusCodeToResult(string protocolName, int statusCode) => $"{protocolName} {statusCode.ToString()[0]}xx";

		/// <summary>
		/// Determines a name from the route values
		/// </summary>
		/// <remarks>
		/// Based on: https://github.com/Microsoft/ApplicationInsights-aspnetcore
		/// </remarks>
		internal static string GetNameFromRouteContext(IDictionary<string, object> routeValues)
		{
			if (routeValues.Count <= 0)
				return null;

			string name = null;
			var count = routeValues.TryGetValue("controller", out var controller) ? 1 : 0;
			var controllerString = controller == null ? string.Empty : controller.ToString();

			if (!string.IsNullOrEmpty(controllerString))
			{
				// Check for MVC areas
				string areaString = null;
				if (routeValues.TryGetValue("area", out var area))
				{
					count++;
					areaString = area.ToString();
				}

				name = !string.IsNullOrEmpty(areaString)
					? areaString + "/" + controllerString
					: controllerString;

				count = routeValues.TryGetValue("action", out var action) ? count + 1 : count;
				var actionString = action == null ? string.Empty : action.ToString();

				if (!string.IsNullOrEmpty(actionString))
					name += "/" + actionString;

				// if there are no other key/values other than area/controller/action, skip parsing parameters
				if (routeValues.Keys.Count == count)
					return name;

				// Add parameters
				var sortedKeys = routeValues.Keys
					.Where(key =>
						!string.Equals(key, "area", StringComparison.OrdinalIgnoreCase) &&
						!string.Equals(key, "controller", StringComparison.OrdinalIgnoreCase) &&
						!string.Equals(key, "action", StringComparison.OrdinalIgnoreCase) &&
						!string.Equals(key, "!__route_group", StringComparison.OrdinalIgnoreCase))
					.OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
					.ToArray();

				if (sortedKeys.Length <= 0)
					return name;

				var arguments = string.Join(@"/", sortedKeys);
				name += " {" + arguments + "}";
			}
			else
			{
				routeValues.TryGetValue("page", out var page);
				var pageString = page == null ? string.Empty : page.ToString();
				if (!string.IsNullOrEmpty(pageString))
					name = pageString;
			}

			return name;
		}

		public void CaptureErrorLog(ErrorLog errorLog, string parentId = null, Exception exception = null, Dictionary<string, Label> labels = null)
			=> ExecutionSegmentCommon.CaptureErrorLog(
				errorLog,
				_sender,
				_logger,
				this,
				ConfigSnapshot,
				this,
				null,
				_apmServerInfo,
				exception,
				labels
			);

		public void SetLabel(string key, string value)
			=> _context.Value.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, bool value)
			=> _context.Value.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, double value)
			=> _context.Value.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, int value)
			=> _context.Value.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, long value)
			=> _context.Value.InternalLabels.Value.InnerDictionary[key] = value;

		public void SetLabel(string key, decimal value)
			=> _context.Value.InternalLabels.Value.InnerDictionary[key] = value;
	}
}
