// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Report.Serialization;
using Newtonsoft.Json;

namespace Elastic.Apm.Model
{
	internal abstract class ExecutionSegment : IExecutionSegment
	{
		protected readonly IApmLogger Logger;

		private readonly ChildDurationTimer _childDurations = new ChildDurationTimer();

		public ExecutionSegment(string name, IApmLogger logger)
		{
			Name = name;
			Timestamp = TimeUtils.TimestampNow();

			Logger = logger?.Scoped($"{GetType().Name}.{Id}");
		}

		public abstract void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null);

		public abstract void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null);

		protected abstract Span CreateSpan(string name, string type, InstrumentationFlag instrumentationFlag);

		protected abstract void InternalEnd(bool isFirstEndCall, long endTimestamp);

		public abstract Dictionary<string, string> Labels { get; }

		public abstract string Name { get; set; }

		public abstract DistributedTracingData OutgoingDistributedTracingData { get; }

		protected abstract string SegmentName { get; }

		private bool _isEnded;

		/// <inheritdoc />
		public double? Duration { get; set; }

		public double? SelfDuration => Duration != null ? Duration - (_childDurations.Duration / 1000.0) : null;

		/// <inheritdoc />
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		public string Id { get; set; }

		/// <inheritdoc />
		[JsonProperty("sampled")]
		public virtual bool IsSampled { get; protected set; }

		/// <inheritdoc />
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("parent_id")]
		public string ParentId { get; set; }

		/// <summary>
		/// Recorded time of the event, UTC based and formatted as microseconds since Unix epoch
		/// </summary>
		public long Timestamp { get; }

		/// <inheritdoc />
		[JsonConverter(typeof(TrimmedStringJsonConverter))]
		[JsonProperty("trace_id")]
		public string TraceId { get; set; }

		public void CaptureSpan(string name, string type, Action<ISpan> capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), capturedAction);

		public void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), capturedAction);

		public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), func);

		public T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), func);

		public Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), func);

		public Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(StartSpan(name, type, subType, action), func);

		public void End()
		{
			if (Duration.HasValue)
			{
				Logger.Trace()
					?.Log($"Ended {{{SegmentName}}} (with Duration already set)." +
						" Start time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp, Duration);
			}
			else
			{
				Assertion.IfEnabled?.That(!_isEnded,
					$"{SegmentName}'s Duration doesn't have value even though {nameof(End)} method was already called." +
					$" It contradicts the invariant enforced by {nameof(End)} method - Duration should have value when {nameof(End)} method exits" +
					$" and {nameof(_isEnded)} field is set to true only when {nameof(End)} method exits." +
					$" Context: this: {this}; {nameof(_isEnded)}: {_isEnded}");

				var endTimestamp = TimeUtils.TimestampNow();
				Duration = TimeUtils.DurationBetweenTimestamps(Timestamp, endTimestamp);
				Logger.Trace()
					?.Log($"Ended {{{SegmentName}}}. Start time: {{Time}} (as timestamp: {{Timestamp}})," +
						" End time: {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
						this, TimeUtils.FormatTimestampForLog(Timestamp), Timestamp,
						TimeUtils.FormatTimestampForLog(endTimestamp), endTimestamp, Duration);
			}

			var calculatedEndTimestamp = Timestamp + (long)(Duration.Value * 1000);

			_childDurations.OnSegmentEnd(calculatedEndTimestamp);

			var isFirstEndCall = !_isEnded;
			_isEnded = true;

			InternalEnd(isFirstEndCall, calculatedEndTimestamp);
		}

		public ISpan StartSpan(string name, string type, string subType = null, string action = null)
			=> StartSpanInternal(name, type, subType, action);

		internal Span StartSpanInternal(string name, string type, string subType = null, string action = null,
			InstrumentationFlag instrumentationFlag = InstrumentationFlag.None
		)
		{
			var span = CreateSpan(name, type, instrumentationFlag);

			if (!string.IsNullOrEmpty(subType)) span.Subtype = subType;

			if (!string.IsNullOrEmpty(action)) span.Action = action;

			Logger.Trace()?.Log("Starting {SpanDetails}", span.ToString());

			return span;
		}

		public void OnChildStart(long timestamp) => _childDurations.OnChildStart(timestamp);

		public void OnChildEnd(long timestamp) => _childDurations.OnChildEnd(timestamp);

		private class ChildDurationTimer
		{
			private int _activeChildren;
			private long _duration;
			private long _start;

			public long Duration => Interlocked.Read(ref _duration);

			public void OnChildStart(long startTimestamp)
			{
				if (Interlocked.Increment(ref _activeChildren) == 1) Interlocked.Exchange(ref _start, startTimestamp);
			}

			public void OnChildEnd(long endTimestamp)
			{
				if (Interlocked.Decrement(ref _activeChildren) == 0) IncrementDuration(endTimestamp);
			}

			public void OnSegmentEnd(long endTimestamp)
			{
				if (Interlocked.Exchange(ref _activeChildren, 0) != 0) IncrementDuration(endTimestamp);
			}

			private void IncrementDuration(long epochMicros) => Interlocked.Add(ref _duration, epochMicros - Interlocked.Read(ref _start));
		}
	}
}
