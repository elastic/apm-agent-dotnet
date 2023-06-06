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
	internal class CentralConfiguration : AbstractConfigurationReader, IConfigurationLookup, IConfigurationDescription
	{
		private const string ThisClassName = nameof(CentralConfigurationFetcher) + "." + nameof(CentralConfiguration);

		private readonly CentralConfigPayload _configPayload;

		public CentralConfiguration(IApmLogger logger, CentralConfigPayload configPayload, string eTag) :
			base(logger,
				ThisClassName)
		{
			_configPayload = configPayload;
			ETag = eTag;

			CaptureBody = GetConfigurationValue(DynamicConfigurationOption.CaptureBody, ParseCaptureBody);
			CaptureBodyContentTypes = GetConfigurationValue(DynamicConfigurationOption.CaptureBodyContentTypes, ParseCaptureBodyContentTypes);
			TransactionMaxSpans = GetSimpleConfigurationValue(DynamicConfigurationOption.TransactionMaxSpans, ParseTransactionMaxSpans);
			TransactionSampleRate = GetSimpleConfigurationValue(DynamicConfigurationOption.TransactionSampleRate, ParseTransactionSampleRate);
			CaptureHeaders = GetSimpleConfigurationValue(DynamicConfigurationOption.CaptureHeaders, ParseCaptureHeaders);
			LogLevel = GetSimpleConfigurationValue(DynamicConfigurationOption.LogLevel, ParseLogLevel);
			SpanStackTraceMinDurationInMilliseconds =
				GetSimpleConfigurationValue(DynamicConfigurationOption.SpanStackTraceMinDuration, ParseSpanStackTraceMinDurationInMilliseconds);
			// Disable obsolete-warning
#pragma warning disable CS0618
			SpanFramesMinDurationInMilliseconds =
				GetSimpleConfigurationValue(DynamicConfigurationOption.SpanFramesMinDuration, ParseSpanFramesMinDurationInMilliseconds);
			// Disable obsolete-warning
#pragma warning restore CS0618
			StackTraceLimit = GetSimpleConfigurationValue(DynamicConfigurationOption.StackTraceLimit, ParseStackTraceLimit);
			Recording = GetSimpleConfigurationValue(DynamicConfigurationOption.Recording, ParseRecording);
			SanitizeFieldNames = GetConfigurationValue(DynamicConfigurationOption.SanitizeFieldNames, ParseSanitizeFieldNames);
			TransactionIgnoreUrls = GetConfigurationValue(DynamicConfigurationOption.TransactionIgnoreUrls, ParseTransactionIgnoreUrls);
			IgnoreMessageQueues = GetConfigurationValue(DynamicConfigurationOption.IgnoreMessageQueues, ParseIgnoreMessageQueues);
			SpanCompressionEnabled = GetSimpleConfigurationValue(DynamicConfigurationOption.SpanCompressionEnabled, ParseSpanCompressionEnabled);
			SpanCompressionExactMatchMaxDuration =
				GetSimpleConfigurationValue(DynamicConfigurationOption.SpanCompressionExactMatchMaxDuration, ParseSpanCompressionExactMatchMaxDuration);
			SpanCompressionSameKindMaxDuration =
				GetSimpleConfigurationValue(DynamicConfigurationOption.SpanCompressionSameKindMaxDuration, ParseSpanCompressionSameKindMaxDuration);
			ExitSpanMinDuration =
				GetSimpleConfigurationValue(DynamicConfigurationOption.ExitSpanMinDuration, ParseExitSpanMinDuration);
			TraceContinuationStrategy =
				GetConfigurationValue(DynamicConfigurationOption.TraceContinuationStrategy, ParseTraceContinuationStrategy);
		}

		public string Description => $"Central configuration (ETag: `{ETag}')";

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

		private CentralConfigurationKeyValue BuildKv(DynamicConfigurationOption option, string value) => new(option, value, Description);

		private T GetConfigurationValue<T>(DynamicConfigurationOption option, Func<ConfigurationKeyValue, T> parser)
			where T : class =>
			_configPayload[option.ToJsonKey()]?.Let(value => parser(BuildKv(option, value)));

		private T? GetSimpleConfigurationValue<T>(DynamicConfigurationOption option, Func<ConfigurationKeyValue, T> parser)
			where T : struct =>
			_configPayload[option.ToJsonKey()]?.Let(value => parser(BuildKv(option, value)));

		public override string ToString()
		{
			var builder = new ToStringBuilder($"[ETag: `{ETag}']");

			if (CaptureBody != null)
				builder.Add(nameof(CaptureBody), CaptureBody);
			if (CaptureBodyContentTypes != null)
				builder.Add(nameof(CaptureBodyContentTypes), string.Join(", ", CaptureBodyContentTypes.Select(x => $"`{x}'")));
			if (TransactionMaxSpans.HasValue)
				builder.Add(nameof(TransactionMaxSpans), TransactionMaxSpans.Value);
			if (TransactionSampleRate.HasValue)
				builder.Add(nameof(TransactionSampleRate), TransactionSampleRate.Value);
			if (Recording.HasValue)
				builder.Add(nameof(Recording), Recording.Value);

			return builder.ToString();
		}

		public ConfigurationKeyValue Lookup(ConfigurationOption option)
		{
			var dynamicOption = option.ToDynamicConfigurationOption();
			return !dynamicOption.HasValue
				? null
				: new CentralConfigurationKeyValue(dynamicOption.Value, _configPayload[dynamicOption.Value.ToJsonKey()], Description);
		}
	}
}
