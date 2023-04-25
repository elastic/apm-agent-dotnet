// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using static Elastic.Apm.BackendComm.CentralConfig.CentralConfigurationResponseParser;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal class CentralConfiguration : AbstractConfigurationReader
	{
		private const string ThisClassName = nameof(CentralConfigurationFetcher) + "." + nameof(CentralConfiguration);

		private readonly CentralConfigPayload _configPayload;

		public CentralConfiguration(IApmLogger logger, CentralConfigPayload configPayload, string eTag) :
			base(logger,
				ThisClassName)
		{
			_configPayload = configPayload;
			ETag = eTag;

			CaptureBody = GetConfigurationValue(CentralConfigPayload.CaptureBodyKey, ParseCaptureBody);
			CaptureBodyContentTypes = GetConfigurationValue(CentralConfigPayload.CaptureBodyContentTypesKey, ParseCaptureBodyContentTypes);
			TransactionMaxSpans = GetSimpleConfigurationValue(CentralConfigPayload.TransactionMaxSpansKey, ParseTransactionMaxSpans);
			TransactionSampleRate = GetSimpleConfigurationValue(CentralConfigPayload.TransactionSampleRateKey, ParseTransactionSampleRate);
			CaptureHeaders = GetSimpleConfigurationValue(CentralConfigPayload.CaptureHeadersKey, ParseCaptureHeaders);
			LogLevel = GetSimpleConfigurationValue(CentralConfigPayload.LogLevelKey, ParseLogLevel);
			SpanStackTraceMinDurationInMilliseconds =
				GetSimpleConfigurationValue(CentralConfigPayload.SpanStackTraceMinDurationKey, ParseSpanStackTraceMinDurationInMilliseconds);
// Disable obsolete-warning
#pragma warning disable CS0618
			SpanFramesMinDurationInMilliseconds =
				GetSimpleConfigurationValue(CentralConfigPayload.SpanFramesMinDurationKey, ParseSpanFramesMinDurationInMilliseconds);
// Disable obsolete-warning
#pragma warning restore CS0618
			StackTraceLimit = GetSimpleConfigurationValue(CentralConfigPayload.StackTraceLimitKey, ParseStackTraceLimit);
			Recording = GetSimpleConfigurationValue(CentralConfigPayload.Recording, ParseRecording);
			SanitizeFieldNames = GetConfigurationValue(CentralConfigPayload.SanitizeFieldNames, ParseSanitizeFieldNames);
			TransactionIgnoreUrls = GetConfigurationValue(CentralConfigPayload.TransactionIgnoreUrls, ParseTransactionIgnoreUrls);
			IgnoreMessageQueues = GetConfigurationValue(CentralConfigPayload.IgnoreMessageQueues, ParseIgnoreMessageQueues);
			SpanCompressionEnabled = GetSimpleConfigurationValue(CentralConfigPayload.SpanCompressionEnabled, ParseSpanCompressionEnabled);
			SpanCompressionExactMatchMaxDuration =
				GetSimpleConfigurationValue(CentralConfigPayload.SpanCompressionExactMatchMaxDuration, ParseSpanCompressionExactMatchMaxDuration);
			SpanCompressionSameKindMaxDuration =
				GetSimpleConfigurationValue(CentralConfigPayload.SpanCompressionSameKindMaxDuration, ParseSpanCompressionSameKindMaxDuration);
			ExitSpanMinDuration =
				GetSimpleConfigurationValue(CentralConfigPayload.ExitSpanMinDuration, ParseExitSpanMinDuration);
			TraceContinuationStrategy =
				GetConfigurationValue(CentralConfigPayload.TraceContinuationStrategy, ParseTraceContinuationStrategy);
		}

		internal string ETag { get; }

		internal string CaptureBody { get; private set; }

		internal List<string> CaptureBodyContentTypes { get; private set; }

		internal bool? CaptureHeaders { get; private set; }

		internal double? ExitSpanMinDuration { get; private set; }

		internal IReadOnlyList<WildcardMatcher> IgnoreMessageQueues { get; private set; }

		internal LogLevel? LogLevel { get; private set; }

		internal bool? Recording { get; private set; }

		internal IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; private set; }

		internal bool? SpanCompressionEnabled { get; private set; }

		internal double? SpanCompressionExactMatchMaxDuration { get; private set; }

		internal double? SpanCompressionSameKindMaxDuration { get; private set; }

		internal double? SpanStackTraceMinDurationInMilliseconds { get; private set; }

		internal double? SpanFramesMinDurationInMilliseconds { get; private set; }

		internal int? StackTraceLimit { get; private set; }

		internal string TraceContinuationStrategy { get; private set; }

		internal IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls { get; private set; }

		internal int? TransactionMaxSpans { get; private set; }

		internal double? TransactionSampleRate { get; private set; }

		private ConfigurationKeyValue BuildKv(string key, string value) => new(key, value, /* readFrom */ $"Central configuration (ETag: `{ETag}')");

		private T GetConfigurationValue<T>(string configurationKey, Func<ConfigurationKeyValue, T> parser)
			where T : class =>
			_configPayload[configurationKey]?.Let(value => parser(BuildKv(configurationKey, value)));

		private T? GetSimpleConfigurationValue<T>(string configurationKey, Func<ConfigurationKeyValue, T> parser)
			where T : struct =>
			_configPayload[configurationKey]?.Let(value => parser(BuildKv(configurationKey, value)));

		public override string ToString()
		{
			var builder = new ToStringBuilder($"[ETag: `{ETag}']");

			if (CaptureBody != null) builder.Add(nameof(CaptureBody), CaptureBody);
			if (CaptureBodyContentTypes != null)
				builder.Add(nameof(CaptureBodyContentTypes), string.Join(", ", CaptureBodyContentTypes.Select(x => $"`{x}'")));
			if (TransactionMaxSpans.HasValue) builder.Add(nameof(TransactionMaxSpans), TransactionMaxSpans.Value);
			if (TransactionSampleRate.HasValue) builder.Add(nameof(TransactionSampleRate), TransactionSampleRate.Value);
			if (Recording.HasValue) builder.Add(nameof(Recording), Recording.Value);

			return builder.ToString();
		}
	}
}
