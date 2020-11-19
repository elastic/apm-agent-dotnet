// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Api;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// A span implementation which is used when the agent is not recording (either recording=false or enabled=false).
	/// It has no knowledge about the PayloadSender and will be never sent to APM Server.
	/// It only executes minimum amount of code and isn't guaranteed that values you set on it will be kept.
	/// </summary>
	internal class NoopSpan : ISpan
	{
		private static readonly SpanContext ReusableContextInstance = new SpanContext();

		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;

		private readonly ISpan _parentSpan;

		internal NoopSpan(string name, string type, string subtype, string action,
			ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer, string parentId = null, string traceId = null, ISpan
				parentSpan = null
		)
		{
			Name = name;
			Type = type;
			ParentId = parentId;
			TraceId = traceId;
			Subtype = subtype;
			Action = action;

			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;
			_currentExecutionSegmentsContainer.CurrentSpan = this;
			_parentSpan = parentSpan;
		}

		public string Action { get; set; }
		public SpanContext Context => ReusableContextInstance;

		public double? Duration { get; set; }
		public string Id { get; }
		public bool IsSampled { get; }
		public Dictionary<string, string> Labels { get; }
		public string Name { get; set; }
		public Outcome Outcome { get; set; }
		public DistributedTracingData OutgoingDistributedTracingData => null;
		public string ParentId { get; }
		public List<CapturedStackFrame> StackTrace { get; }
		public string Subtype { get; set; }
		public long Timestamp { get; }
		public string TraceId { get; }
		public string TransactionId { get; }
		public string Type { get; set; }

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null, Dictionary<string, Label> labels = null
		) { }

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null,
			Dictionary<string, Label> labels = null
		) { }

		public void CaptureSpan(string name, string type, Action<ISpan> capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				capturedAction);

		public void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				capturedAction);

		public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				func);

		public T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				func);

		public Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				func);

		public Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this),
				func);

		public void End() => _currentExecutionSegmentsContainer.CurrentSpan = _parentSpan;

		public void SetLabel(string key, string value) { }

		public void SetLabel(string key, bool value) { }

		public void SetLabel(string key, double value) { }

		public void SetLabel(string key, int value) { }

		public void SetLabel(string key, long value) { }

		public void SetLabel(string key, decimal value) { }

		public Label GetLabel(string key) => null;

		public ISpan StartSpan(string name, string type, string subType = null, string action = null) =>
			new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer, Id, TraceId, this);
	}
}
