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

		public string CaptureBody => _content.CaptureBody;
		public List<string> CaptureBodyContentTypes => _content.CaptureBodyContentTypes;
		public bool CaptureHeaders => _content.CaptureHeaders;
		public bool CentralConfig => _content.CentralConfig;
		public string DbgDescription { get; }
		public IReadOnlyList<WildcardMatcher> DisableMetrics => _content.DisableMetrics;
		public string Environment => _content.Environment;
		public TimeSpan FlushInterval => _content.FlushInterval;
		public IReadOnlyDictionary<string, string> GlobalLabels => _content.GlobalLabels;
		public LogLevel LogLevel => _content.LogLevel;
		public int MaxBatchEventCount => _content.MaxBatchEventCount;
		public int MaxQueueEventCount => _content.MaxQueueEventCount;
		public double MetricsIntervalInMilliseconds => _content.MetricsIntervalInMilliseconds;

		public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => _content.SanitizeFieldNames;
		public string SecretToken => _content.SecretToken;
		public string ApiKey => _content.ApiKey;
		public IReadOnlyList<Uri> ServerUrls => _content.ServerUrls;
		public string ServiceName => _content.ServiceName;
		public string ServiceNodeName => _content.ServiceNodeName;
		public string ServiceVersion => _content.ServiceVersion;
		public double SpanFramesMinDurationInMilliseconds => _content.SpanFramesMinDurationInMilliseconds;
		public int StackTraceLimit => _content.StackTraceLimit;
		public int TransactionMaxSpans => _content.TransactionMaxSpans;
		public double TransactionSampleRate => _content.TransactionSampleRate;
		public bool UseElasticTraceparentHeader => _content.UseElasticTraceparentHeader;
		public bool VerifyServerCert => _content.VerifyServerCert;
		public IReadOnlyCollection<string> ExcludedNamespaces => _content.ExcludedNamespaces;
		public IReadOnlyCollection<string> ApplicationNamespaces => _content.ApplicationNamespaces;
	}
}
