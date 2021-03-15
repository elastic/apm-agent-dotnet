// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

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

		public string ApiKey => _content.ApiKey;
		public IReadOnlyCollection<string> ApplicationNamespaces => _content.ApplicationNamespaces;

		public string CaptureBody => _content.CaptureBody;
		public List<string> CaptureBodyContentTypes => _content.CaptureBodyContentTypes;
		public bool CaptureHeaders => _content.CaptureHeaders;
		public bool CentralConfig => _content.CentralConfig;
		public string CloudProvider => _content.CloudProvider;
		public string DbgDescription { get; }
		public IReadOnlyList<WildcardMatcher> DisableMetrics => _content.DisableMetrics;
		public bool Enabled => _content.Enabled;
		public string Environment => _content.Environment;
		public IReadOnlyCollection<string> ExcludedNamespaces => _content.ExcludedNamespaces;
		public TimeSpan FlushInterval => _content.FlushInterval;
		public IReadOnlyDictionary<string, string> GlobalLabels => _content.GlobalLabels;
		public string HostName => _content.HostName;
		public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues => _content.IgnoreMessageQueues;
		public LogLevel LogLevel => _content.LogLevel;
		public int MaxBatchEventCount => _content.MaxBatchEventCount;
		public int MaxQueueEventCount => _content.MaxQueueEventCount;
		public double MetricsIntervalInMilliseconds => _content.MetricsIntervalInMilliseconds;
		public bool Recording => _content.Recording;
		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => _content.SanitizeFieldNames;
		public string SecretToken => _content.SecretToken;
		[Obsolete("Use ServerUrl")]
		public IReadOnlyList<Uri> ServerUrls => _content.ServerUrls;
		public string ServerCert => _content.ServerCert;
		public Uri ServerUrl => _content.ServerUrl;
		public string ServiceName => _content.ServiceName;
		public string ServiceNodeName => _content.ServiceNodeName;
		public string ServiceVersion => _content.ServiceVersion;
		public double SpanFramesMinDurationInMilliseconds => _content.SpanFramesMinDurationInMilliseconds;
		public int StackTraceLimit => _content.StackTraceLimit;
		public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls => _content.TransactionIgnoreUrls;
		public int TransactionMaxSpans => _content.TransactionMaxSpans;
		public double TransactionSampleRate => _content.TransactionSampleRate;
		public bool UseElasticTraceparentHeader => _content.UseElasticTraceparentHeader;
		public bool VerifyServerCert => _content.VerifyServerCert;
	}
}
