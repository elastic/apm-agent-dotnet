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
	internal class CentralConfigReader : AbstractConfigurationReader
	{
		private const string ThisClassName = nameof(CentralConfigFetcher) + "." + nameof(CentralConfigReader);

		private readonly CentralConfigResponseParser.CentralConfigPayload _configPayload;

		public CentralConfigReader(IApmLogger logger, CentralConfigResponseParser.CentralConfigPayload configPayload, string eTag) : base(logger,
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

		internal LogLevel? LogLevel { get; private set; }

		internal bool? Recording { get; private set; }

		internal IReadOnlyList<WildcardMatcher> SanitizeFieldNames { get; private set; }

		internal double? SpanFramesMinDurationInMilliseconds { get; private set; }

		internal int? StackTraceLimit { get; private set; }

		internal int? TransactionMaxSpans { get; private set; }

		internal double? TransactionSampleRate { get; private set; }

		private void UpdateConfigurationValues()
		{
			CaptureBody = GetConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.CaptureBodyKey, ParseCaptureBody);
			CaptureBodyContentTypes = GetConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.CaptureBodyContentTypesKey,
				ParseCaptureBodyContentTypes);
			TransactionMaxSpans = GetSimpleConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.TransactionMaxSpansKey,
				ParseTransactionMaxSpans);
			TransactionSampleRate = GetSimpleConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.TransactionSampleRateKey,
				ParseTransactionSampleRate);
			CaptureHeaders = GetSimpleConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.CaptureHeadersKey, ParseCaptureHeaders);
			LogLevel = GetSimpleConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.LogLevelKey, ParseLogLevel);
			SpanFramesMinDurationInMilliseconds =
				GetSimpleConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.SpanFramesMinDurationKey,
					ParseSpanFramesMinDurationInMilliseconds);
			StackTraceLimit = GetSimpleConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.StackTraceLimitKey, ParseStackTraceLimit);
			Recording = GetSimpleConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.Recording, ParseRecording);
			SanitizeFieldNames =
				GetConfigurationValue(CentralConfigResponseParser.CentralConfigPayload.SanitizeFieldNames, ParseSanitizeFieldNames);
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
