// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Elastic.Apm.Helpers;
using Elastic.Apm.Libraries.Newtonsoft.Json;
using Elastic.Apm.Logging;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal class CentralConfigurationResponseParser : ICentralConfigurationResponseParser
	{
		internal static readonly TimeSpan WaitTimeIfNoCacheControlMaxAge = TimeSpan.FromMinutes(5);
		private readonly IApmLogger _logger;

		internal CentralConfigurationResponseParser(IApmLogger logger) => _logger = logger?.Scoped(nameof(CentralConfigurationResponseParser));

		public (CentralConfigurationReader, CentralConfigurationFetcher.WaitInfoS) ParseHttpResponse(HttpResponseMessage httpResponse,
			string httpResponseBody
		)
		{
			_logger.Trace()
				?.Log("Processing HTTP response..."
					+ Environment.NewLine + "+-> Response:{HttpResponse}"
					+ Environment.NewLine + "+-> Response body [length: {HttpResponseBodyLength}]:{HttpResponseBody}"
					, httpResponse == null ? " N/A" : Environment.NewLine + TextUtils.Indent(httpResponse.ToString())
					, httpResponseBody == null ? "N/A" : httpResponseBody.Length.ToString()
					, httpResponseBody == null ? " N/A" : Environment.NewLine + TextUtils.Indent(httpResponseBody));

			var waitInfo = ExtractWaitInfo(httpResponse);
			try
			{
				if (!InterpretResponseStatusCode(httpResponse, waitInfo)) return (null, waitInfo);

				if (httpResponse?.Headers?.ETag == null)
					throw new CentralConfigurationFetcher.FailedToFetchConfigException("Response from APM Server doesn't have ETag header", waitInfo);

				var keyValues = JsonConvert.DeserializeObject<IDictionary<string, string>>(httpResponseBody);
				var centralConfigReader = ParseConfigPayload(httpResponse, new CentralConfigPayload(keyValues));

				return (centralConfigReader, waitInfo);
			}
			catch (Exception ex) when (!(ex is CentralConfigurationFetcher.FailedToFetchConfigException))
			{
				throw new CentralConfigurationFetcher.FailedToFetchConfigException("Exception was thrown while parsing response from APM Server",
					waitInfo,
					cause: ex);
			}
		}

		private CentralConfigurationReader ParseConfigPayload(HttpResponseMessage httpResponse, CentralConfigPayload configPayload)
		{
			if (configPayload.UnknownKeys != null && configPayload.UnknownKeys.Any())
			{
				_logger.Info()
					?.Log("Central configuration response contains keys that are not in the list of options"
						+ " that can be changed after Agent start: {UnknownKeys}. Supported options: {ReloadableOptions}."
						, string.Join(", ", configPayload.UnknownKeys.Select(k => $"`[{k.Key}, {k.Value}]'"))
						, string.Join(", ", CentralConfigPayload.SupportedOptions.Select(k => $"`{k}'")));
			}

			var eTag = httpResponse.Headers.ETag.ToString();

			return new CentralConfigurationReader(_logger, configPayload, eTag);
		}

		private static CentralConfigurationFetcher.WaitInfoS ExtractWaitInfo(HttpResponseMessage httpResponse)
		{
			if (httpResponse.Headers?.CacheControl?.MaxAge != null)
			{
				if (httpResponse.Headers.CacheControl.MaxAge <= TimeSpan.FromSeconds(0))
				{
					return new CentralConfigurationFetcher.WaitInfoS(WaitTimeIfNoCacheControlMaxAge,
						"The max-age directive in Cache-Control header in APM Server's response is zero or negative, "
						+ $"which is invalid - falling back to use default ({WaitTimeIfNoCacheControlMaxAge.Minutes} minutes) wait time.");
				}
				if (httpResponse.Headers.CacheControl.MaxAge > TimeSpan.FromSeconds(0) && httpResponse.Headers.CacheControl.MaxAge < TimeSpan.FromSeconds(5))
				{
					return new CentralConfigurationFetcher.WaitInfoS(TimeSpan.FromSeconds(5),
						"The max-age directive in Cache-Control header in APM Server's response is less than 5 seconds, "
						+ "which is less than expected by the spec - falling back to use 5 seconds wait time.");
				}

				return new CentralConfigurationFetcher.WaitInfoS(httpResponse.Headers.CacheControl.MaxAge.Value,
					"Wait time is taken from max-age directive in Cache-Control header in APM Server's response");
			}

			return new CentralConfigurationFetcher.WaitInfoS(WaitTimeIfNoCacheControlMaxAge,
				"Default wait time is used because there's no valid Cache-Control header with max-age directive in APM Server's response."
				+ Environment.NewLine + "+-> Response:" + Environment.NewLine + TextUtils.Indent(httpResponse.ToString()));
		}

		private bool InterpretResponseStatusCode(HttpResponseMessage httpResponse, CentralConfigurationFetcher.WaitInfoS waitInfo)
		{
			if (httpResponse.IsSuccessStatusCode) return true;

			var severity = LogLevel.Error;
			var statusAsString = $"HTTP status code is {httpResponse.ReasonPhrase} ({(int)httpResponse.StatusCode})";
			string message;

			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (httpResponse.StatusCode)
			{
				case HttpStatusCode.NotModified: // 304
					_logger.Trace()
						?.Log("HTTP status code is {HttpResponseReasonPhrase} ({HttpStatusCode})"
							+ " which means the configuration has not changed since the previous fetch."
							+ " Response:{NewLine}{HttpResponse}"
							, httpResponse.ReasonPhrase
							, (int)httpResponse.StatusCode
							, Environment.NewLine
							, TextUtils.Indent(httpResponse.ToString()));
					return false;

				case HttpStatusCode.BadRequest: // 400
					message = $"{statusAsString} which is unexpected";
					break;

				case HttpStatusCode.Forbidden: // 403
					severity = LogLevel.Debug;
					message = $"{statusAsString} which most likely means that APM Server supports the central configuration "
						+ "endpoint but Kibana connection is not enabled";
					break;

				case HttpStatusCode.NotFound: // 404
					severity = LogLevel.Debug;
					message = $"{statusAsString} which most likely means that APM Server is an old (pre 7.3) version which "
						+ "doesn't support the central configuration endpoint";
					break;

				case HttpStatusCode.ServiceUnavailable: // 503
					message = $"{statusAsString} which most likely means that APM Server supports the central configuration "
						+ "endpoint and Kibana connection is enabled, but Kibana connection is unavailable";
					break;

				default:
					message = $"{statusAsString} signifies a failure";
					break;
			}

			throw new CentralConfigurationFetcher.FailedToFetchConfigException(message, waitInfo, severity);
		}

		internal class CentralConfigPayload
		{
			internal const string CaptureBodyContentTypesKey = "capture_body_content_types";
			internal const string CaptureBodyKey = "capture_body";
			internal const string CaptureHeadersKey = "capture_headers";
			internal const string ExitSpanMinDuration = "exit_span_min_duration";
			internal const string IgnoreMessageQueues = "ignore_message_queues";
			internal const string LogLevelKey = "log_level";
			internal const string Recording = "recording";
			internal const string SanitizeFieldNames = "sanitize_field_names";
			internal const string SpanCompressionEnabled = "span_compression_enabled";
			internal const string SpanCompressionExactMatchMaxDuration = "span_compression_exact_match_max_duration";
			internal const string SpanCompressionSameKindMaxDuration = "span_compression_same_kind_max_duration";
			internal const string SpanStackTraceMinDurationKey = "span_stack_trace_min_duration";
			[Obsolete("Use SpanStackTraceMinDurationKey")]
			internal const string SpanFramesMinDurationKey = "span_frames_min_duration";
			internal const string StackTraceLimitKey = "stack_trace_limit";
			internal const string TraceContinuationStrategy = "trace_continuation_strategy";
			internal const string TransactionIgnoreUrls = "transaction_ignore_urls";
			internal const string TransactionMaxSpansKey = "transaction_max_spans";
			internal const string TransactionSampleRateKey = "transaction_sample_rate";

			internal static readonly ISet<string> SupportedOptions = new HashSet<string>
			{
				CaptureBodyKey,
				CaptureBodyContentTypesKey,
				CaptureHeadersKey,
				IgnoreMessageQueues,
				LogLevelKey,
				Recording,
				SanitizeFieldNames,
				SpanStackTraceMinDurationKey,
// Disable obsolete-warning
#pragma warning disable CS0618
				SpanFramesMinDurationKey,
#pragma warning restore CS0618
				StackTraceLimitKey,
				TransactionIgnoreUrls,
				TransactionMaxSpansKey,
				TransactionSampleRateKey,
				SpanCompressionEnabled,
				SpanCompressionExactMatchMaxDuration,
				SpanCompressionSameKindMaxDuration,
				ExitSpanMinDuration
			};

			private readonly IDictionary<string, string> _keyValues;

			public CentralConfigPayload(IDictionary<string, string> keyValues) => _keyValues = keyValues;

			public string this[string key]
			{
				get
				{
					_keyValues.TryGetValue(key, out var val);
					return val;
				}
			}

			[JsonIgnore]
			public IEnumerable<KeyValuePair<string, string>> UnknownKeys => _keyValues.Where(x => !SupportedOptions.Contains(x.Key));
		}
	}
}
