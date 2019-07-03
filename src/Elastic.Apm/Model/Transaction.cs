using System;
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
			Timestamp = TimeUtils.TimestampNow();
			var idBytes = new byte[8];
			Id = RandomGenerator.GenerateRandomBytesAsString(idBytes);
			_logger = logger?.Scoped($"{nameof(Transaction)}.{Id}");

			_sender = sender;

			Name = name;
			HasCustomName = false;
			Type = type;

			var isSamplingFromDistributedTracingData = false;
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
					"IsSampled ({IsSampled}) is based on incoming distributed tracing data ({DistributedTracingData})." +
					" Start time: {Time} (as timestamp: {Timestamp})",
					this, IsSampled, distributedTracingData, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
			else
				_logger.Trace()?.Log("New Transaction instance created: {Transaction}. " +
					"IsSampled ({IsSampled}) is based on the given sampler ({Sampler})." +
					" Start time: {Time} (as timestamp: {Timestamp})",
					this, IsSampled, sampler, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
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
		public double? Duration { get; private set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; }

		[JsonProperty("sampled")]
		public bool IsSampled { get; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Name
		{
			get => _name;
			set
			{
				HasCustomName = true;
				_name = value;
			}
		}

		private string _name;

		/// <summary>
		/// If true, then the transaction name was modified by external code, and transaction name should not be changed
		/// or "fixed" automatically ref https://github.com/elastic/apm-agent-dotnet/pull/258.
		/// </summary>
		[JsonIgnore]
		internal bool HasCustomName { get; private set; }

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

		/// <summary>
		/// Recorded time of the event, UTC based and formatted as microseconds since Unix epoch
		/// </summary>
		public long Timestamp { get; }

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

		public void End(double? duration = null)
		{
			if (Duration.HasValue)
			{
				_logger.Debug()?.Log("End() called on already ended {Transaction}. Start time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}",
					this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, Duration);
			}
			else
			{
				if (duration.HasValue)
				{
					Duration = duration.Value;
					_logger.Trace()?.Log("Ended {Transaction}. Start time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, Duration);
				}
				else
				{
					var endTimestamp = TimeUtils.TimestampNow();
					Duration = TimeUtils.DurationBetweenTimestamps(Timestamp, endTimestamp);
					_logger.Trace()?.Log("Ended {Transaction}. Start time: {Time} (as timestamp: {Timestamp})," +
						" End time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp,
						TimeUtils.FormatTimestampForLog(endTimestamp), endTimestamp, Duration);
				}
			}

			_sender.QueueTransaction(this);

			Agent.TransactionContainer.Transactions.Value = null;
		}

		public ISpan StartSpan(string name, string type, string subType = null, string action = null)
			=> StartSpanInternal(name, type, subType, action);

		internal Span StartSpanInternal(string name, string type, string subType = null, string action = null)
		{
			var retVal = new Span(name, type, Id, TraceId, this, IsSampled, _sender, _logger);

			if (!string.IsNullOrEmpty(subType)) retVal.Subtype = subType;

			if (!string.IsNullOrEmpty(action)) retVal.Action = action;

			_logger.Trace()?.Log("Starting {SpanDetails}", retVal.ToString());
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
