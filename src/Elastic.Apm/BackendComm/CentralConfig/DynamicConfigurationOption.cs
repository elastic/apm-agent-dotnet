// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using Elastic.Apm.Config;
using static Elastic.Apm.BackendComm.CentralConfig.DynamicConfigurationOption;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal enum DynamicConfigurationOption
	{
		CaptureBodyContentTypes = ConfigurationOption.CaptureBodyContentTypes,
		CaptureBody = ConfigurationOption.CaptureBody,
		CaptureHeaders = ConfigurationOption.CaptureHeaders,
		ExitSpanMinDuration = ConfigurationOption.ExitSpanMinDuration,
		IgnoreMessageQueues = ConfigurationOption.IgnoreMessageQueues,
		LogLevel = ConfigurationOption.LogLevel,
		Recording = ConfigurationOption.Recording,
		SanitizeFieldNames = ConfigurationOption.SanitizeFieldNames,
		SpanCompressionEnabled = ConfigurationOption.SpanCompressionEnabled,
		SpanCompressionExactMatchMaxDuration = ConfigurationOption.SpanCompressionExactMatchMaxDuration,
		SpanCompressionSameKindMaxDuration = ConfigurationOption.SpanCompressionSameKindMaxDuration,
		SpanStackTraceMinDuration = ConfigurationOption.SpanStackTraceMinDuration,

		[Obsolete("Use SpanStackTraceMinDurationKey")]
		SpanFramesMinDuration = ConfigurationOption.SpanFramesMinDuration,
		StackTraceLimit = ConfigurationOption.StackTraceLimit,
		TraceContinuationStrategy = ConfigurationOption.TraceContinuationStrategy,
		TransactionIgnoreUrls = ConfigurationOption.TransactionIgnoreUrls,
		TransactionMaxSpans = ConfigurationOption.TransactionMaxSpans,
		TransactionSampleRate = ConfigurationOption.TransactionSampleRate,
	}

	internal static class DynamicConfigurationExtensions
	{
		private static readonly IReadOnlyCollection<DynamicConfigurationOption> All =
			(DynamicConfigurationOption[])Enum.GetValues(typeof(DynamicConfigurationOption));

		public static IReadOnlyCollection<DynamicConfigurationOption> AllDynamicOptions() => All;

		public static ConfigurationOption ToConfigurationOption(this DynamicConfigurationOption dynamicOption) =>
			dynamicOption switch
			{
				CaptureBodyContentTypes => ConfigurationOption.CaptureBodyContentTypes,
				CaptureBody => ConfigurationOption.CaptureBody,
				CaptureHeaders => ConfigurationOption.CaptureHeaders,
				ExitSpanMinDuration => ConfigurationOption.ExitSpanMinDuration,
				IgnoreMessageQueues => ConfigurationOption.IgnoreMessageQueues,
				DynamicConfigurationOption.LogLevel => ConfigurationOption.LogLevel,
				Recording => ConfigurationOption.Recording,
				SanitizeFieldNames => ConfigurationOption.SanitizeFieldNames,
				SpanCompressionEnabled => ConfigurationOption.SpanCompressionEnabled,
				SpanCompressionExactMatchMaxDuration => ConfigurationOption.SpanCompressionExactMatchMaxDuration,
				SpanCompressionSameKindMaxDuration => ConfigurationOption.SpanCompressionSameKindMaxDuration,
				SpanStackTraceMinDuration => ConfigurationOption.SpanStackTraceMinDuration,
#pragma warning disable CS0618
				SpanFramesMinDuration => ConfigurationOption.SpanFramesMinDuration,
#pragma warning restore CS0618
				StackTraceLimit => ConfigurationOption.StackTraceLimit,
				TraceContinuationStrategy => ConfigurationOption.TraceContinuationStrategy,
				TransactionIgnoreUrls => ConfigurationOption.TransactionIgnoreUrls,
				TransactionMaxSpans => ConfigurationOption.TransactionMaxSpans,
				TransactionSampleRate => ConfigurationOption.TransactionSampleRate,
				_ => throw new ArgumentOutOfRangeException(nameof(dynamicOption), dynamicOption, null)
			};

		public static DynamicConfigurationOption? ToDynamicConfigurationOption(this ConfigurationOption option) =>
			option switch
			{
				ConfigurationOption.CaptureBodyContentTypes => CaptureBodyContentTypes,
				ConfigurationOption.CaptureBody => CaptureBody,
				ConfigurationOption.CaptureHeaders => CaptureHeaders,
				ConfigurationOption.ExitSpanMinDuration => ExitSpanMinDuration,
				ConfigurationOption.IgnoreMessageQueues => IgnoreMessageQueues,
				ConfigurationOption.LogLevel => DynamicConfigurationOption.LogLevel,
				ConfigurationOption.Recording => Recording,
				ConfigurationOption.SanitizeFieldNames => SanitizeFieldNames,
				ConfigurationOption.SpanCompressionEnabled => SpanCompressionEnabled,
				ConfigurationOption.SpanCompressionExactMatchMaxDuration => SpanCompressionExactMatchMaxDuration,
				ConfigurationOption.SpanCompressionSameKindMaxDuration => SpanCompressionSameKindMaxDuration,
				ConfigurationOption.SpanStackTraceMinDuration => SpanStackTraceMinDuration,
#pragma warning disable CS0618
				ConfigurationOption.SpanFramesMinDuration => SpanFramesMinDuration,
#pragma warning restore CS0618
				ConfigurationOption.StackTraceLimit => StackTraceLimit,
				ConfigurationOption.TraceContinuationStrategy => TraceContinuationStrategy,
				ConfigurationOption.TransactionIgnoreUrls => TransactionIgnoreUrls,
				ConfigurationOption.TransactionMaxSpans => TransactionMaxSpans,
				ConfigurationOption.TransactionSampleRate => TransactionSampleRate,
				_ => null
			};

		public static string ToJsonKey(this DynamicConfigurationOption dynamicOption) =>
			dynamicOption switch
			{
				CaptureBodyContentTypes => "capture_body_content_types",
				CaptureBody => "capture_body",
				CaptureHeaders => "capture_headers",
				ExitSpanMinDuration => "exit_span_min_duration",
				IgnoreMessageQueues => "ignore_message_queues",
				DynamicConfigurationOption.LogLevel => "log_level",
				Recording => "recording",
				SanitizeFieldNames => "sanitize_field_names",
				SpanCompressionEnabled => "span_compression_enabled",
				SpanCompressionExactMatchMaxDuration => "span_compression_exact_match_max_duration",
				SpanCompressionSameKindMaxDuration => "span_compression_same_kind_max_duration",
				SpanStackTraceMinDuration => "span_stack_trace_min_duration",
#pragma warning disable CS0618
				SpanFramesMinDuration => "span_frames_min_duration",
#pragma warning restore CS0618
				StackTraceLimit => "stack_trace_limit",
				TraceContinuationStrategy => "trace_continuation_strategy",
				TransactionIgnoreUrls => "transaction_ignore_urls",
				TransactionMaxSpans => "transaction_max_spans",
				TransactionSampleRate => "transaction_sample_rate",
				_ => throw new ArgumentOutOfRangeException(nameof(dynamicOption), dynamicOption, null)
			};
	}
}
