using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elastic.Apm.BackendComm
{
	internal class CentralConfigFetcher : BackendCommComponentBase, ICentralConfigFetcher
	{
		private const string ThisClassName = nameof(CentralConfigFetcher);

		internal static readonly TimeSpan GetConfigHttpRequestTimeout = TimeSpan.FromMinutes(5);

		internal static readonly TimeSpan WaitTimeIfAnyError = TimeSpan.FromMinutes(5);
		internal static readonly TimeSpan WaitTimeIfNoCacheControlMaxAge = TimeSpan.FromMinutes(5);

		private readonly IAgentTimer _agentTimer;
		private readonly IConfigStore _configStore;
		private readonly Uri _getConfigAbsoluteUrl;
		private readonly IConfigSnapshot _initialSnapshot;
		private readonly IApmLogger _logger;

		internal CentralConfigFetcher(IApmLogger logger, IConfigStore configStore, Service service
			, HttpMessageHandler httpMessageHandler = null, IAgentTimer agentTimer = null, string dbgName = null
		)
			: this(logger, configStore, configStore.CurrentSnapshot, service, httpMessageHandler, agentTimer, dbgName) { }

		/// <summary>
		/// We need this private ctor to avoid calling configStore.CurrentSnapshot twice (and thus possibly using different
		/// snapshots)
		/// when passing isEnabled: initialConfigSnapshot.CentralConfig and config: initialConfigSnapshot to base
		/// </summary>
		private CentralConfigFetcher(IApmLogger logger, IConfigStore configStore, IConfigSnapshot initialConfigSnapshot, Service service
			, HttpMessageHandler httpMessageHandler, IAgentTimer agentTimer, string dbgName
		)
			: base( /* isEnabled: */ initialConfigSnapshot.CentralConfig, logger, ThisClassName, service, initialConfigSnapshot, httpMessageHandler)
		{
			_logger = logger?.Scoped(ThisClassName + (dbgName == null ? "" : $" (dbgName: `{dbgName}')"));

			_initialSnapshot = initialConfigSnapshot;

			var isCentralConfigOptEqDefault = _initialSnapshot.CentralConfig == ConfigConsts.DefaultValues.CentralConfig;
			var centralConfigStatus = _initialSnapshot.CentralConfig ? "enabled" : "disabled";
			if (!isCentralConfigOptEqDefault) centralConfigStatus = centralConfigStatus.ToUpper();
			_logger.IfLevel(isCentralConfigOptEqDefault ? LogLevel.Debug : LogLevel.Information)
				?.Log("Central configuration feature is {CentralConfigStatus} because CentralConfig option's value is {CentralConfigOptionValue}"
					+ " (default value is {CentralConfigOptionDefaultValue})"
					, centralConfigStatus, _initialSnapshot.CentralConfig, ConfigConsts.DefaultValues.CentralConfig);

			if (!_initialSnapshot.CentralConfig) return;

			_configStore = configStore;

			_agentTimer = agentTimer ?? new AgentTimer();

			_getConfigAbsoluteUrl = BackendCommUtils.ApmServerEndpoints.BuildGetConfigAbsoluteUrl(initialConfigSnapshot.ServerUrls.First(), service);
			_logger.Debug()
				?.Log("Combined absolute URL for APM Server get central configuration endpoint: `{Url}'. Service: {Service}."
					, _getConfigAbsoluteUrl, service);

			StartWorkLoop();
		}

		private long _dbgIterationsCount;

		private EntityTagHeaderValue _eTag;

		protected override async Task WorkLoopIteration()
		{
			++_dbgIterationsCount;
			var waitingLogSeverity = LogLevel.Trace;
			WaitInfoS waitInfo;
			HttpRequestMessage httpRequest = null;
			HttpResponseMessage httpResponse = null;
			string httpResponseBody = null;
			try
			{
				httpRequest = BuildHttpRequest(_eTag);

				(httpResponse, httpResponseBody) = await FetchConfigHttpResponseAsync(httpRequest);

				ConfigDelta configDelta;
				(configDelta, waitInfo) = ProcessHttpResponse(httpResponse, httpResponseBody);
				if (configDelta != null)
				{
					UpdateConfigStore(configDelta);
					_eTag = httpResponse.Headers.ETag;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var severity = LogLevel.Error;
				waitInfo = new WaitInfoS(WaitTimeIfAnyError, "Default wait time is used because exception was thrown"
					+ " while fetching configuration from APM Server and parsing it.");

				if (ex is FailedToFetchConfigException fEx)
				{
					severity = fEx.Severity;
					fEx.WaitInfo?.Let(it => { waitInfo = it; });
				}

				if (severity == LogLevel.Error) waitingLogSeverity = LogLevel.Information;

				_logger.IfLevel(severity)
					?.LogException(ex, "Exception was thrown while fetching configuration from APM Server and parsing it."
						+ " ETag: `{ETag}'. URL: `{Url}'. Apm Server base URL: `{ApmServerUrl}'. WaitInterval: {WaitInterval}."
						+ " dbgIterationsCount: {dbgIterationsCount}."
						+ Environment.NewLine + "+-> Request:{HttpRequest}"
						+ Environment.NewLine + "+-> Response:{HttpResponse}"
						+ Environment.NewLine + "+-> Response body [length: {HttpResponseBodyLength}]:{HttpResponseBody}"
						, _eTag.AsNullableToString(), _getConfigAbsoluteUrl, HttpClientInstance.BaseAddress, waitInfo.Interval.ToHms(),
						_dbgIterationsCount
						, httpRequest == null ? " N/A" : Environment.NewLine + TextUtils.Indent(httpRequest.ToString())
						, httpResponse == null ? " N/A" : Environment.NewLine + TextUtils.Indent(httpResponse.ToString())
						, httpResponseBody == null ? "N/A" : httpResponseBody.Length.ToString()
						, httpResponseBody == null ? " N/A" : Environment.NewLine + TextUtils.Indent(httpResponseBody));
			}
			finally
			{
				httpRequest?.Dispose();
				httpResponse?.Dispose();
			}

			_logger.IfLevel(waitingLogSeverity)
				?.Log("Waiting {WaitInterval}... {WaitReason}. dbgIterationsCount: {dbgIterationsCount}."
					, waitInfo.Interval.ToHms(), waitInfo.Reason, _dbgIterationsCount);
			await _agentTimer.Delay(_agentTimer.Now + waitInfo.Interval, CtsInstance.Token);
		}

		private HttpRequestMessage BuildHttpRequest(EntityTagHeaderValue eTag)
		{
			var httpRequest = new HttpRequestMessage(HttpMethod.Get, _getConfigAbsoluteUrl);
			if (eTag != null) httpRequest.Headers.IfNoneMatch.Add(eTag);
			return httpRequest;
		}

		private async Task<ValueTuple<HttpResponseMessage, string>> FetchConfigHttpResponseImplAsync(HttpRequestMessage httpRequest)
		{
			_logger.Trace()?.Log("Making HTTP request to APM Server... Request: {HttpRequest}.", httpRequest);

			var httpResponse = await HttpClientInstance.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, CtsInstance.Token);
			// ReSharper disable once InvertIf
			if (httpResponse == null)
			{
				throw new FailedToFetchConfigException("HTTP client API call for request to APM Server returned null."
					+ $" Request:{Environment.NewLine}{TextUtils.Indent(httpRequest.ToString())}");
			}

			_logger.Trace()?.Log("Reading HTTP response body... Response: {HttpResponse}.", httpResponse);
			var httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

			return (httpResponse, httpResponseBody);
		}

		private async Task<ValueTuple<HttpResponseMessage, string>> FetchConfigHttpResponseAsync(HttpRequestMessage httpRequest) =>
			await _agentTimer.AwaitOrTimeout(FetchConfigHttpResponseImplAsync(httpRequest)
				, _agentTimer.Now + GetConfigHttpRequestTimeout, CtsInstance.Token);

		private (ConfigDelta, WaitInfoS) ProcessHttpResponse(HttpResponseMessage httpResponse, string httpResponseBody)
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
				if (!InterpretResponseStatusCode(httpResponse)) return (null, waitInfo);

				var configPayload = JsonConvert.DeserializeObject<ConfigPayload>(httpResponseBody);
				var configDelta = ParseConfigPayload(httpResponse, configPayload);

				return (configDelta, waitInfo);
			}
			catch (FailedToFetchConfigException ex)
			{
				ex.WaitInfo = waitInfo;
				throw;
			}
			catch (Exception ex)
			{
				throw new FailedToFetchConfigException("Exception was thrown while parsing response from APM Server", cause: ex)
				{
					WaitInfo = waitInfo
				};
			}
		}

		private bool InterpretResponseStatusCode(HttpResponseMessage httpResponse)
		{
			if (httpResponse.IsSuccessStatusCode) return true;

			var statusCode = (int)httpResponse.StatusCode;
			var severity = 400 <= statusCode && statusCode < 500 ? LogLevel.Debug : LogLevel.Error;

			string message;
			var msgPrefix = $"HTTP status code is {httpResponse.ReasonPhrase} which most likely means that ";
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (httpResponse.StatusCode)
			{
				case HttpStatusCode.NotModified: // 304
					_logger.Trace()
						?.Log("HTTP status code is {HttpResponseReasonPhrase}"
							+ " which means the configuration has not changed since the previous fetch."
							+ " Response:{NewLine}{HttpResponse}"
							, httpResponse.ReasonPhrase, Environment.NewLine, TextUtils.Indent(httpResponse.ToString()));
					return false;

				case HttpStatusCode.BadRequest: // 400
					severity = LogLevel.Error;
					message = $"HTTP status code is {httpResponse.ReasonPhrase} which is unexpected";
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
					message = $"HTTP status code ({httpResponse.ReasonPhrase}) signifies a failure";
					break;
			}

			throw new FailedToFetchConfigException(message, severity);
		}

		private ConfigDelta ParseConfigPayload(HttpResponseMessage httpResponse, ConfigPayload configPayload)
		{
			if (httpResponse.Headers?.ETag == null)
				throw new FailedToFetchConfigException("Response from APM Server doesn't have ETag header");

			var eTag = httpResponse.Headers.ETag.ToString();

			var configParser = new ConfigParser(_logger, configPayload, eTag);

			if (configPayload.UnknownKeys != null && !configPayload.UnknownKeys.IsEmpty())
			{
				_logger.Info()
					?.Log("Central configuration response contains keys that are not in the list of options"
						+ " that can be changed after Agent start: {UnknownKeys}. Supported options: {ReloadableOptions}."
						, string.Join(", ", configPayload.UnknownKeys.Select(kv => $"`{kv.Key}'"))
						, string.Join(", ", ConfigPayload.SupportedOptions.Select(k => $"`{k}'")));
			}

			return new ConfigDelta(transactionSampleRate: configParser.TransactionSampleRate, eTag: eTag);
		}

		private static WaitInfoS ExtractWaitInfo(HttpResponseMessage httpResponse)
		{
			if (httpResponse.Headers?.CacheControl?.MaxAge != null)
			{
				return new WaitInfoS(httpResponse.Headers.CacheControl.MaxAge.Value,
					"Wait time is taken from max-age directive in Cache-Control header in APM Server's response");
			}

			return new WaitInfoS(WaitTimeIfNoCacheControlMaxAge,
				"Default wait time is used because there's no valid Cache-Control header with max-age directive in APM Server's response."
				+ Environment.NewLine + "+-> Response:" + Environment.NewLine + TextUtils.Indent(httpResponse.ToString()));
		}

		private void UpdateConfigStore(ConfigDelta configDelta)
		{
			_logger.Info()?.Log("Updating " + nameof(ConfigStore) + ". New central configuration: {ConfigDelta}", configDelta);

			_configStore.CurrentSnapshot = new WrappingConfigSnapshot(_initialSnapshot, configDelta
				, $"{_initialSnapshot.DbgDescription} + central (ETag: `{configDelta.ETag}')");
		}

		private class FailedToFetchConfigException : Exception
		{
			internal FailedToFetchConfigException(string message, LogLevel severity = LogLevel.Error, Exception cause = null)
				: base(message, cause) => Severity = severity;

			internal LogLevel Severity { get; }
			internal WaitInfoS? WaitInfo { get; set; }
		}

		private readonly struct WaitInfoS
		{
			internal readonly TimeSpan Interval;
			internal readonly string Reason;

			internal WaitInfoS(TimeSpan interval, string reason)
			{
				Interval = interval;
				Reason = reason;
			}
		}

		private class ConfigPayload
		{
			internal const string TransactionSampleRateKey = "transaction_sample_rate";

			internal static readonly string[] SupportedOptions = { TransactionSampleRateKey };

			[JsonProperty(TransactionSampleRateKey)]
			public string TransactionSampleRate { get; set; }

			[JsonExtensionData]
			// ReSharper disable once UnusedAutoPropertyAccessor.Local
			// ReSharper disable once CollectionNeverUpdated.Local
			public IDictionary<string, JToken> UnknownKeys { get; set; }
		}

		private class ConfigDelta
		{
			internal ConfigDelta(string eTag, double? transactionSampleRate)
			{
				ETag = eTag;
				TransactionSampleRate = transactionSampleRate;
			}

			internal string ETag { get; }

			internal double? TransactionSampleRate { get; }

			public override string ToString()
			{
				var builder = new ToStringBuilder($"[ETag: `{ETag}']");

				if (TransactionSampleRate.HasValue) builder.Add(nameof(TransactionSampleRate), TransactionSampleRate.Value);

				return builder.ToString();
			}
		}

		private class ConfigParser : AbstractConfigurationReader
		{
			// ReSharper disable once MemberHidesStaticFromOuterClass
			private const string ThisClassName = nameof(CentralConfigFetcher) + "." + nameof(ConfigParser);

			private readonly ConfigPayload _configPayload;
			private readonly string _eTag;

			public ConfigParser(IApmLogger logger, ConfigPayload configPayload, string eTag) : base(logger, ThisClassName)
			{
				_configPayload = configPayload;
				_eTag = eTag;
			}

			internal double? TransactionSampleRate => _configPayload.TransactionSampleRate?.Let(
				value => ParseTransactionSampleRate(BuildKv(ConfigPayload.TransactionSampleRateKey, value)));

			private ConfigurationKeyValue BuildKv(string key, string value) =>
				new ConfigurationKeyValue(key, value, /* readFrom */ $"Central configuration (ETag: `{_eTag}')");
		}

		private class WrappingConfigSnapshot : IConfigSnapshot
		{
			private readonly ConfigDelta _configDelta;
			private readonly IConfigSnapshot _wrapped;

			internal WrappingConfigSnapshot(IConfigSnapshot wrapped, ConfigDelta configDelta, string dbgDescription)
			{
				_wrapped = wrapped;
				_configDelta = configDelta;
				DbgDescription = dbgDescription;
			}

			public string CaptureBody => _wrapped.CaptureBody;

			public List<string> CaptureBodyContentTypes => _wrapped.CaptureBodyContentTypes;

			public bool CaptureHeaders => _wrapped.CaptureHeaders;
			public bool CentralConfig => _wrapped.CentralConfig;

			public string DbgDescription { get; }

			public string Environment => _wrapped.Environment;

			public TimeSpan FlushInterval => _wrapped.FlushInterval;

			public LogLevel LogLevel => _wrapped.LogLevel;

			public int MaxBatchEventCount => _wrapped.MaxBatchEventCount;

			public int MaxQueueEventCount => _wrapped.MaxQueueEventCount;

			public double MetricsIntervalInMilliseconds => _wrapped.MetricsIntervalInMilliseconds;

			public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => _wrapped.SanitizeFieldNames;

			public string SecretToken => _wrapped.SecretToken;

			public IReadOnlyList<Uri> ServerUrls => _wrapped.ServerUrls;

			public string ServiceName => _wrapped.ServiceName;

			public string ServiceVersion => _wrapped.ServiceVersion;

			public double SpanFramesMinDurationInMilliseconds => _wrapped.SpanFramesMinDurationInMilliseconds;

			public int StackTraceLimit => _wrapped.StackTraceLimit;

			public int TransactionMaxSpans => _wrapped.TransactionMaxSpans;

			public double TransactionSampleRate => _configDelta.TransactionSampleRate ?? _wrapped.TransactionSampleRate;
		}
	}
}
