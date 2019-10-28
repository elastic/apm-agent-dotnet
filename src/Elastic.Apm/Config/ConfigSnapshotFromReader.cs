using System;
using System.Collections.Generic;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.Config
{
	internal class ConfigSnapshotFromReader : IConfigSnapshot
	{
		private readonly IConfigurationReader _content;

		internal ConfigSnapshotFromReader(IConfigurationReader content, string dbgDescription)
		{
			_content = content;
			DbgDescription = dbgDescription;
		}

		public string CaptureBody => _content.CaptureBody;
		public List<string> CaptureBodyContentTypes => _content.CaptureBodyContentTypes;
		public bool CaptureHeaders => _content.CaptureHeaders;
		public bool CentralConfig => _content.CentralConfig;
		public string DbgDescription { get; }
		public string Environment => _content.Environment;
		public TimeSpan FlushInterval => _content.FlushInterval;
		public LogLevel LogLevel => _content.LogLevel;
		public int MaxBatchEventCount => _content.MaxBatchEventCount;
		public int MaxQueueEventCount => _content.MaxQueueEventCount;
		public double MetricsIntervalInMilliseconds => _content.MetricsIntervalInMilliseconds;

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => _content.SanitizeFieldNames;
		public string SecretToken => _content.SecretToken;
		public IReadOnlyList<Uri> ServerUrls => _content.ServerUrls;
		public string ServiceName => _content.ServiceName;
		public string ServiceVersion => _content.ServiceVersion;
		public double SpanFramesMinDurationInMilliseconds => _content.SpanFramesMinDurationInMilliseconds;
		public int StackTraceLimit => _content.StackTraceLimit;
		public int TransactionMaxSpans => _content.TransactionMaxSpans;
		public double TransactionSampleRate => _content.TransactionSampleRate;
	}
}
