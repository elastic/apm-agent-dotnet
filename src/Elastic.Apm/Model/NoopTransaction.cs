// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Api.Constraints;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Model
{
	/// <summary>
	/// A transaction implementation which is used when the agent is not recording (either recording=false or enabled=false).
	/// It has no knowledge about the PayloadSender and will be never sent to APM Server.
	/// It only executes minimum amount of code and isn't guaranteed that values you set on it will be kept.
	/// </summary>
	internal class NoopTransaction : ITransaction
	{
		private static DefaultConfigurationSnapshot _defaultConfigurationSnapshot = new DefaultConfigurationSnapshot();
		private static readonly Context ReusableContextInstance = new Context();
		private readonly ICurrentExecutionSegmentsContainer _currentExecutionSegmentsContainer;

		private readonly Lazy<Dictionary<string, string>> _custom = new Lazy<Dictionary<string, string>>();

		private readonly Lazy<Dictionary<string, string>> _labels = new Lazy<Dictionary<string, string>>();

		public NoopTransaction(string name, string type, ICurrentExecutionSegmentsContainer currentExecutionSegmentsContainer)
		{
			Name = name;
			Type = type;
			_currentExecutionSegmentsContainer = currentExecutionSegmentsContainer;
			_currentExecutionSegmentsContainer.CurrentTransaction = this;
		}

		public Context Context =>
			ReusableContextInstance;

		public Dictionary<string, string> Custom => _custom.Value;

		public IConfigurationSnapshot ConfigurationSnapshot => _defaultConfigurationSnapshot;
		public double? Duration { get; set; }

		[MaxLength]
		public string Id { get; }

		public bool IsSampled => false;
		public Dictionary<string, string> Labels => _labels.Value;

		[MaxLength]
		public string Name { get; set; }

		public Outcome Outcome { get; set; }
		public DistributedTracingData OutgoingDistributedTracingData { get; }

		[JsonProperty("parent_id")]
		[MaxLength]
		public string ParentId { get; }

		[MaxLength]
		public string Result { get; set; }

		[JsonProperty("span_count")]
		public SpanCount SpanCount { get; }

		[JsonProperty("trace_id")]
		[MaxLength]
		public string TraceId { get; }

		[MaxLength]
		public string Type { get; set; }

		public void CaptureError(string message, string culprit, StackFrame[] frames, string parentId = null, Dictionary<string, Label> labels = null
		) { }

		public void CaptureException(Exception exception, string culprit = null, bool isHandled = false, string parentId = null,
			Dictionary<string, Label> labels = null
		) { }

		public void CaptureSpan(string name, string type, Action<ISpan> capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), capturedAction);

		public void CaptureSpan(string name, string type, Action capturedAction, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), capturedAction);

		public T CaptureSpan<T>(string name, string type, Func<ISpan, T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), func);

		public T CaptureSpan<T>(string name, string type, Func<T> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), func);

		public Task CaptureSpan(string name, string type, Func<Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), func);

		public Task CaptureSpan(string name, string type, Func<ISpan, Task> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), func);

		public Task<T> CaptureSpan<T>(string name, string type, Func<ISpan, Task<T>> func, string subType = null, string action = null)
			=> ExecutionSegmentCommon.CaptureSpan(new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer), func);

		public void End() => _currentExecutionSegmentsContainer.CurrentTransaction = null;

		public void SetLabel(string key, string value) { }

		public void SetLabel(string key, bool value) { }

		public void SetLabel(string key, double value) { }

		public void SetLabel(string key, int value) { }

		public void SetLabel(string key, long value) { }

		public void SetLabel(string key, decimal value) { }

		public ISpan StartSpan(string name, string type, string subType = null, string action = null) =>
			new NoopSpan(name, type, subType, action, _currentExecutionSegmentsContainer);

		public string EnsureParentId() => string.Empty;

		public void SetService(string serviceName, string serviceVersion) { }

		public bool TryGetLabel<T>(string key, out T value)
		{
			value = default;
			return false;
		}

		public void CaptureErrorLog(ErrorLog errorLog, string parentId = null, Exception exception = null, Dictionary<string, Label> labels = null
		) { }
	}

	/// <summary>
	/// A static config snapshot which contains default values
	/// </summary>
	internal class DefaultConfigurationSnapshot : IConfigurationSnapshot
	{
		public string ApiKey => string.Empty;
		public IReadOnlyCollection<string> ApplicationNamespaces => new List<string>();
		public string CaptureBody => ConfigConsts.DefaultValues.CaptureBody;
		public List<string> CaptureBodyContentTypes => new();
		public bool CaptureHeaders => ConfigConsts.DefaultValues.CaptureHeaders;
		public bool CentralConfig => ConfigConsts.DefaultValues.CentralConfig;
		public string CloudProvider => ConfigConsts.DefaultValues.CloudProvider;
		public IReadOnlyList<WildcardMatcher> DisableMetrics => new List<WildcardMatcher>();
		public bool Enabled => false;
		public string Environment => string.Empty;
		public IReadOnlyCollection<string> ExcludedNamespaces => new List<string>();
		public TimeSpan FlushInterval => TimeSpan.FromMilliseconds(ConfigConsts.DefaultValues.FlushIntervalInMilliseconds);
		public IReadOnlyDictionary<string, string> GlobalLabels => new Dictionary<string, string>();
		public string HostName => string.Empty;
		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues => new Collection<WildcardMatcher>();
		public LogLevel LogLevel => LogLevel.Error;
		public int MaxBatchEventCount => ConfigConsts.DefaultValues.MaxBatchEventCount;
		public int MaxQueueEventCount => ConfigConsts.DefaultValues.MaxQueueEventCount;
		public double MetricsIntervalInMilliseconds => ConfigConsts.DefaultValues.MetricsIntervalInMilliseconds;
		public bool Recording => false;
		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => new List<WildcardMatcher>();
		public string SecretToken => string.Empty;
		public string ServerCert => string.Empty;
		public IReadOnlyList<Uri> ServerUrls => new List<Uri>();
		public Uri ServerUrl => ConfigConsts.DefaultValues.ServerUri;
		public string ServiceName => string.Empty;
		public string ServiceNodeName => string.Empty;
		public string ServiceVersion => string.Empty;
		public double SpanFramesMinDurationInMilliseconds => ConfigConsts.DefaultValues.SpanFramesMinDurationInMilliseconds;
		public int StackTraceLimit => ConfigConsts.DefaultValues.StackTraceLimit;
		public bool TraceContextIgnoreSampledFalse => ConfigConsts.DefaultValues.TraceContextIgnoreSampledFalse;
		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls => new Collection<WildcardMatcher>();
		public int TransactionMaxSpans => ConfigConsts.DefaultValues.TransactionMaxSpans;


		public double TransactionSampleRate => ConfigConsts.DefaultValues.TransactionSampleRate;
		public bool UseElasticTraceparentHeader => false;
		public bool VerifyServerCert => false;
		public string DbgDescription => nameof(DefaultConfigurationSnapshot);
	}
}
