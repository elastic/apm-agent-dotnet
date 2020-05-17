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
	internal class Transaction : ExecutionSegment, ITransaction
	{
		private readonly Lazy<Context> _context = new Lazy<Context>();
		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;

		private readonly IPayloadSender _sender;

		private readonly string _traceState;

		// This constructor is meant for serialization
		//[JsonConstructor]
		// private Transaction(Context context, string name, string type, double duration, long timestamp, string id, string traceId, string parentId,
		// 	bool isSampled, string result, SpanCount spanCount
		// )
		// {
		// 	_context = new Lazy<Context>(() => context);
		// 	Name = name;
		// 	Duration = duration;
		// 	Timestamp = timestamp;
		// 	Type = type;
		// 	Id = id;
		// 	TraceId = traceId;
		// 	ParentId = parentId;
		// 	IsSampled = isSampled;
		// 	Result = result;
		// 	SpanCount = spanCount;
		// }

		// This constructor is used only by tests that don't care about sampling and distributed tracing
		internal Transaction(ApmAgent agent, string name, string type)
			: this(agent.Logger, name, type, new Sampler(1.0), null, agent.PayloadSender, agent.ConfigStore.CurrentSnapshot,
				agent.TracerInternal.CurrentExecutionSegmentsContainer) { }

		internal Transaction(
			IApmLogger logger,
			string name,
			string type,
			Sampler sampler,
			DistributedTracingData distributedTracingData,
			IPayloadSender sender,
			IConfigSnapshot configSnapshot,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer
		) : base(name, logger, configSnapshot)
		{
			var idBytes = new byte[8];
			Id = RandomGenerator.GenerateRandomBytesAsString(idBytes);

			_sender = sender;
			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;

			Type = type;

			StartActivity();

			var isSamplingFromDistributedTracingData = false;
			if (distributedTracingData == null)
			{
				// Here we ignore Activity.Current.ActivityTraceFlags because it starts out without setting the IsSampled flag, so relying on that would mean a transaction is never sampled.
				IsSampled = sampler.DecideIfToSample(idBytes);

				if (Activity.Current != null && Activity.Current.IdFormat == ActivityIdFormat.W3C)
				{
					TraceId = Activity.Current.TraceId.ToString();
					ParentId = Activity.Current.ParentId;

					// Also mark the sampling decision on the Activity
					if (IsSampled)
						Activity.Current.ActivityTraceFlags |= ActivityTraceFlags.Recorded;
				}
				else
					TraceId = _activity.TraceId.ToString();
			}
			else
			{
				TraceId = distributedTracingData.TraceId;
				ParentId = distributedTracingData.ParentId;
				IsSampled = distributedTracingData.FlagRecorded;
				isSamplingFromDistributedTracingData = true;
				_traceState = distributedTracingData.TraceState;
			}

			SpanCount = new SpanCount();

			_currentExecutionSegmentsContainer.CurrentTransaction = this;

			if (isSamplingFromDistributedTracingData)
			{
				Logger.Trace()
					?.Log("New Transaction instance created: {Transaction}. " +
						"IsSampled ({IsSampled}) is based on incoming distributed tracing data ({DistributedTracingData})." +
						" Start time: {Time} (as timestamp: {Timestamp})",
						this, IsSampled, distributedTracingData, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
			}
			else
			{
				Logger.Trace()
					?.Log("New Transaction instance created: {Transaction}. " +
						"IsSampled ({IsSampled}) is based on the given sampler ({Sampler})." +
						" Start time: {Time} (as timestamp: {Timestamp})",
						this, IsSampled, sampler, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp);
			}

			void StartActivity()
			{
				_activity = new Activity("ElasticApm.Transaction");
				_activity.SetIdFormat(ActivityIdFormat.W3C);
				_activity.Start();
			}
		}

		/// <summary>
		/// The agent also starts an Activity when a transaction is started and stops it when the transaction ends.
		/// The TraceId of this activity is always the same as the TraceId of the transaction.
		/// With this, in case Activity.Current is null, the agent will set it and when the next Activity gets created it'll
		/// have this activity as its parent and the TraceId will flow to all Activity instances.
		/// </summary>
		private Activity _activity;

		/// <summary>
		/// Any arbitrary contextual information regarding the event, captured by the agent, optionally provided by the user.
		/// <seealso cref="ShouldSerializeContext" />
		/// </summary>
		public Context Context => _context.Value;

		[JsonIgnore]
		public Dictionary<string, string> Custom => Context.Custom;

		[JsonIgnore]
		internal bool IsContextCreated => _context.IsValueCreated;

		/// <inheritdoc />
		[JsonProperty("sampled")]
		public override bool IsSampled { get; }

		[JsonIgnore]
		public override Dictionary<string, string> Labels => Context.Labels;

		[JsonIgnore]
		public override DistributedTracingData OutgoingDistributedTracingData => new DistributedTracingData(TraceId, Id, IsSampled, _traceState);

		/// <inheritdoc />
		/// <summary>
		/// A string describing the result of the transaction.
		/// This is typically the HTTP status code, or e.g. "success" for a background task.
		/// </summary>
		/// <value>The result.</value>
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Result { get; set; }

		protected override string SegmentName => "Transaction";

		internal Service Service;

		[JsonProperty("span_count")]
		public SpanCount SpanCount { get; set; }

		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Type { get; set; }

		public string EnsureParentId()
		{
			if (!string.IsNullOrEmpty(ParentId))
				return ParentId;

			var idBytes = new byte[8];
			ParentId = RandomGenerator.GenerateRandomBytesAsString(idBytes);
			Logger?.Debug()?.Log("Setting ParentId to transaction, {transaction}", this);
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
			{ nameof(IsSampled), IsSampled }
		}.ToString();

		protected override void InternalEnd(bool isFirstEndCall, long endTimestamp)
		{
			_activity?.Stop();

			if (!isFirstEndCall) return;

			_sender.QueueTransaction(this);
			_currentExecutionSegmentsContainer.CurrentTransaction = null;
		}

		protected override Span CreateSpan(string name, string type, InstrumentationFlag instrumentationFlag) =>
			new Span(name, type, Id, TraceId, this, _sender, Logger, _currentExecutionSegmentsContainer, instrumentationFlag: instrumentationFlag);


		public override void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null)
			=> ExecutionSegmentCommon.CaptureException(
				exception,
				Logger,
				_sender,
				this,
				ConfigSnapshot,
				this,
				culprit,
				isHandled,
				parentId
			);

		public override void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null)
			=> ExecutionSegmentCommon.CaptureError(
				message,
				culprit,
				frames,
				_sender,
				Logger,
				this,
				ConfigSnapshot,
				this,
				parentId
			);

		internal static string StatusCodeToResult(string protocolName, int statusCode) => $"{protocolName} {statusCode.ToString()[0]}xx";
	}
}
