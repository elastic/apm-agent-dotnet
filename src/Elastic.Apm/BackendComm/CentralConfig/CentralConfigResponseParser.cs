// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Newtonsoft.Json;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal class CentralConfigResponseParser : ICentralConfigResponseParser
	{
		private readonly IApmLogger _logger;

		internal static readonly TimeSpan WaitTimeIfNoCacheControlMaxAge = TimeSpan.FromMinutes(5);

		internal CentralConfigResponseParser(IApmLogger logger)
		{
			_logger = logger?.Scoped(nameof(CentralConfigResponseParser));
		}

		public (CentralConfigReader, CentralConfigFetcher.WaitInfoS) ParseHttpResponse(HttpResponseMessage httpResponse,
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
					throw new CentralConfigFetcher.FailedToFetchConfigException("Response from APM Server doesn't have ETag header", waitInfo);

				var keyValues = JsonConvert.DeserializeObject<IDictionary<string, string>>(httpResponseBody);
				var centralConfigReader = ParseConfigPayload(httpResponse, new CentralConfigPayload(keyValues));

				return (centralConfigReader, waitInfo);
			}
			catch (Exception ex) when (!(ex is CentralConfigFetcher.FailedToFetchConfigException))
			{
				throw new CentralConfigFetcher.FailedToFetchConfigException("Exception was thrown while parsing response from APM Server", waitInfo,
					cause: ex);
			}
		}

		private CentralConfigReader ParseConfigPayload(HttpResponseMessage httpResponse, CentralConfigPayload configPayload)
		{
			if (configPayload.UnknownKeys != null && !configPayload.UnknownKeys.IsEmpty())
			{
				_logger.Info()
					?.Log("Central configuration response contains keys that are not in the list of options"
						+ " that can be changed after Agent start: {UnknownKeys}. Supported options: {ReloadableOptions}."
						, string.Join(", ", configPayload.UnknownKeys.Select(k => $"`[{k.Key}, {k.Value}]'"))
						, string.Join(", ", CentralConfigPayload.SupportedOptions.Select(k => $"`{k}'")));
			}

			var eTag = httpResponse.Headers.ETag.ToString();

			return new CentralConfigReader(_logger, configPayload, eTag);
		}

		private static CentralConfigFetcher.WaitInfoS ExtractWaitInfo(HttpResponseMessage httpResponse)
		{
			if (httpResponse.Headers?.CacheControl?.MaxAge != null)
			{
				return new CentralConfigFetcher.WaitInfoS(httpResponse.Headers.CacheControl.MaxAge.Value,
					"Wait time is taken from max-age directive in Cache-Control header in APM Server's response");
			}

			return new CentralConfigFetcher.WaitInfoS(WaitTimeIfNoCacheControlMaxAge,
				"Default wait time is used because there's no valid Cache-Control header with max-age directive in APM Server's response."
				+ Environment.NewLine + "+-> Response:" + Environment.NewLine + TextUtils.Indent(httpResponse.ToString()));
		}

		private bool InterpretResponseStatusCode(HttpResponseMessage httpResponse, CentralConfigFetcher.WaitInfoS waitInfo)
		{
			if (httpResponse.IsSuccessStatusCode) return true;

			var statusCode = (int)httpResponse.StatusCode;
			var severity = 400 <= statusCode && statusCode < 500 ? LogLevel.Debug : LogLevel.Error;

			string message;
			var statusAsString = $"HTTP status code is {httpResponse.ReasonPhrase} ({(int)httpResponse.StatusCode})";
			var msgPrefix = $"{statusAsString} which most likely means that ";
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
					severity = LogLevel.Error;
					message = $"{statusAsString} which is unexpected";
					break;

				case HttpStatusCode.Forbidden: // 403
					message = msgPrefix + "APM Server supports the central configuration endpoint but Kibana connection is not enabled";
					break;

				case HttpStatusCode.NotFound: // 404
					message = msgPrefix + "APM Server is an old (pre 7.3) version which doesn't support the central configuration endpoint";
					break;

				case HttpStatusCode.ServiceUnavailable: // 503
					message = msgPrefix + "APM Server supports the central configuration endpoint and Kibana connection is enabled"
						+ ", but Kibana connection is unavailable";
					break;

				default:
					message = $"{statusAsString} signifies a failure";
					break;
			}

			throw new CentralConfigFetcher.FailedToFetchConfigException(message, waitInfo, severity);
		}

		internal class CentralConfigPayload
		{
			internal const string CaptureBodyContentTypesKey = "capture_body_content_types";
			internal const string CaptureBodyKey = "capture_body";
			internal const string TransactionMaxSpansKey = "transaction_max_spans";
			internal const string TransactionSampleRateKey = "transaction_sample_rate";

			internal const string CaptureHeadersKey = "capture_headers";
			internal const string LogLevelKey = "log_level";
			internal const string SpanFramesMinDurationKey = "span_frames_min_duration";
			internal const string StackTraceLimitKey = "stack_trace_limit";

			internal static readonly ISet<string> SupportedOptions = new HashSet<string>
			{
				CaptureBodyKey, CaptureBodyContentTypesKey, TransactionMaxSpansKey, TransactionSampleRateKey,
				CaptureHeadersKey, LogLevelKey, SpanFramesMinDurationKey, StackTraceLimitKey
			};

			private readonly IDictionary<string, string> _keyValues;

			public CentralConfigPayload(IDictionary<string, string> keyValues)
			{
				_keyValues = keyValues;
			}

			[JsonIgnore]
			public IEnumerable<KeyValuePair<string, string>> UnknownKeys => _keyValues.Where(x => !SupportedOptions.Contains(x.Key));

			public string this[string key] => _keyValues.ContainsKey(key) ? _keyValues[key] : null;
		}
	}
}
