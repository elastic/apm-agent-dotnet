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

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal class CentralConfigurationReader : AbstractConfigurationReader
	{
		private const string ThisClassName = nameof(CentralConfigurationFetcher) + "." + nameof(CentralConfigurationReader);

		private readonly CentralConfigurationResponseParser.CentralConfigPayload _configPayload;

		public CentralConfigurationReader(IApmLogger logger, CentralConfigurationResponseParser.CentralConfigPayload configPayload, string eTag) :
			base(logger,
				ThisClassName)
		{
			_configPayload = configPayload;
			ETag = eTag;

			UpdateConfigurationValues();
		}

		internal string CaptureBody { get; private set; }

		internal List<string> CaptureBodyContentTypes { get; private set; }

		internal bool? CaptureHeaders { get; private set; }

		internal string ETag { get; }

		internal IReadOnlyList<WildcardMatcher> IgnoreMessageQueues { get; private set; }

		internal LogLevel? LogLevel { get; private set; }

		internal bool? Recording { get; private set; }

		internal IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; private set; }

		internal double? SpanFramesMinDurationInMilliseconds { get; private set; }

		internal int? StackTraceLimit { get; private set; }

		internal IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls { get; private set; }

		internal int? TransactionMaxSpans { get; private set; }

		internal double? TransactionSampleRate { get; private set; }

		internal bool? SpanCompressionEnabled { get; private set; }

		internal double? SpanCompressionExactMatchMaxDuration { get; private set; }

		internal double? SpanCompressionSameKindMaxDuration { get; private set; }

		internal double? ExitSpanMinDuration { get; private set; }

		private void UpdateConfigurationValues()
		{
			CaptureBody = GetConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.CaptureBodyKey, ParseCaptureBody);
			CaptureBodyContentTypes = GetConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.CaptureBodyContentTypesKey,
				ParseCaptureBodyContentTypes);
			TransactionMaxSpans = GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.TransactionMaxSpansKey,
				ParseTransactionMaxSpans);
			TransactionSampleRate = GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.TransactionSampleRateKey,
				ParseTransactionSampleRate);
			CaptureHeaders =
				GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.CaptureHeadersKey, ParseCaptureHeaders);
			LogLevel = GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.LogLevelKey, ParseLogLevel);
			SpanFramesMinDurationInMilliseconds =
				GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.SpanFramesMinDurationKey,
					ParseSpanFramesMinDurationInMilliseconds);
			StackTraceLimit = GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.StackTraceLimitKey,
				ParseStackTraceLimit);
			Recording = GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.Recording, ParseRecording);
			SanitizeFieldNames =
				GetConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.SanitizeFieldNames, ParseSanitizeFieldNamesImpl);
			TransactionIgnoreUrls =
				GetConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.TransactionIgnoreUrls, ParseTransactionIgnoreUrlsImpl);
			IgnoreMessageQueues =
				GetConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.IgnoreMessageQueues, ParseIgnoreMessageQueuesImpl);
			SpanCompressionEnabled =
				GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.SpanCompressionEnabled,
					ParseSpanCompressionEnabled);
			SpanCompressionExactMatchMaxDuration =
				GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.SpanCompressionExactMatchMaxDuration,
					ParseSpanCompressionExactMatchMaxDuration);
			SpanCompressionSameKindMaxDuration =
				GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.SpanCompressionSameKindMaxDuration,
					ParseSpanCompressionSameKindMaxDuration);
			ExitSpanMinDuration =
				GetSimpleConfigurationValue(CentralConfigurationResponseParser.CentralConfigPayload.ExitSpanMinDuration, ParseExitSpanMinDuration);
		}

		private ConfigurationKeyValue BuildKv(string key, string value) =>
			new ConfigurationKeyValue(key, value, /* readFrom */ $"Central configuration (ETag: `{ETag}')");

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
