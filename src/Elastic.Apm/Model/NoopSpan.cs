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
	internal class NoopSpan : ISpan
	{
		private static readonly SpanContext ReusableContextInstance = new SpanContext();

		public NoopSpan() { }

		internal NoopSpan(string name, string type, string parentId, string traceId)
		{
			Name = name;
			Type = type;
			ParentId = parentId;
			TraceId = traceId;
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
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), capturedAction);

		public void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), capturedAction);

		public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), func);

		public T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), func);

		public Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), func);

		public Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action), func);

		public void End() { }

		public void SetLabel(string key, string value) { }

		public void SetLabel(string key, bool value) { }

		public void SetLabel(string key, double value) { }

		public void SetLabel(string key, int value) { }

		public void SetLabel(string key, long value) { }

		public void SetLabel(string key, decimal value) { }

		public ISpan StartSpan(string name, string type, string subType = null, string action = null) => new NoopSpan(name, type, subType, action);
	}
}
