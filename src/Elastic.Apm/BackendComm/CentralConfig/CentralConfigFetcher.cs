// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal class CentralConfigFetcher : BackendCommComponentBase, ICentralConfigFetcher
	{
		private const string ThisClassName = nameof(CentralConfigFetcher);

		internal static readonly TimeSpan GetConfigHttpRequestTimeout = TimeSpan.FromMinutes(5);

		internal static readonly TimeSpan WaitTimeIfAnyError = TimeSpan.FromMinutes(5);

		private readonly IAgentTimer _agentTimer;
		private readonly IConfigStore _configStore;
		private readonly Uri _getConfigAbsoluteUrl;
		private readonly IConfigSnapshot _initialSnapshot;
		private readonly IApmLogger _logger;
		private readonly ICentralConfigResponseParser _centralConfigResponseParser;

		internal CentralConfigFetcher(IApmLogger logger, IConfigStore configStore, ICentralConfigResponseParser centralConfigResponseParser,
			Service service,
			HttpMessageHandler httpMessageHandler = null, IAgentTimer agentTimer = null, string dbgName = null
		) : this(logger, configStore, configStore.CurrentSnapshot, service, httpMessageHandler, agentTimer, dbgName) =>
			_centralConfigResponseParser = centralConfigResponseParser;

		internal CentralConfigFetcher(IApmLogger logger, IConfigStore configStore, Service service
			, HttpMessageHandler httpMessageHandler = null, IAgentTimer agentTimer = null, string dbgName = null
		)
			: this(logger, configStore, new CentralConfigResponseParser(logger), service, httpMessageHandler, agentTimer, dbgName) { }

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

				CentralConfigReader centralConfigReader;
				(centralConfigReader, waitInfo) = _centralConfigResponseParser.ParseHttpResponse(httpResponse, httpResponseBody);
				if (centralConfigReader != null)
				{
					UpdateConfigStore(centralConfigReader);
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

				if (ex is FailedToFetchConfigException fEx)
				{
					severity = fEx.Severity;
					waitInfo = fEx.WaitInfo;
				}
				else
				{
					waitInfo = new WaitInfoS(WaitTimeIfAnyError, "Default wait time is used because exception was thrown"
						+ " while fetching configuration from APM Server and parsing it.");
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

		private async Task<(HttpResponseMessage, string)> FetchConfigHttpResponseImplAsync(HttpRequestMessage httpRequest)
		{
			_logger.Trace()?.Log("Making HTTP request to APM Server... Request: {HttpRequest}.", httpRequest);

			var httpResponse = await HttpClientInstance.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, CtsInstance.Token);
			// ReSharper disable once InvertIf
			if (httpResponse == null)
			{
				throw new FailedToFetchConfigException("HTTP client API call for request to APM Server returned null."
					+ $" Request:{Environment.NewLine}{TextUtils.Indent(httpRequest.ToString())}",
					new WaitInfoS(WaitTimeIfAnyError, "HttpResponseMessage from APM Server is equal to null"));
			}

			_logger.Trace()?.Log("Reading HTTP response body... Response: {HttpResponse}.", httpResponse);
			var httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

			return (httpResponse, httpResponseBody);
		}

		private async Task<(HttpResponseMessage, string)> FetchConfigHttpResponseAsync(HttpRequestMessage httpRequest) =>
			await _agentTimer.AwaitOrTimeout(FetchConfigHttpResponseImplAsync(httpRequest)
				, _agentTimer.Now + GetConfigHttpRequestTimeout, CtsInstance.Token);

		private void UpdateConfigStore(CentralConfigReader centralConfigReader)
		{
			_logger.Info()?.Log("Updating " + nameof(ConfigStore) + ". New central configuration: {CentralConfiguration}", centralConfigReader);

			_configStore.CurrentSnapshot = new WrappingConfigSnapshot(_initialSnapshot, centralConfigReader
				, $"{_initialSnapshot.DbgDescription} + central (ETag: `{centralConfigReader.ETag}')");
		}

		internal class FailedToFetchConfigException : Exception
		{
			internal FailedToFetchConfigException(string message, WaitInfoS waitInfo, LogLevel severity = LogLevel.Error, Exception cause = null)
				: base(message, cause)
			{
				Severity = severity;
				WaitInfo = waitInfo;
			}

			internal LogLevel Severity { get; }
			internal WaitInfoS WaitInfo { get; }
		}

		internal readonly struct WaitInfoS
		{
			internal readonly TimeSpan Interval;
			internal readonly string Reason;

			internal WaitInfoS(TimeSpan interval, string reason)
			{
				Interval = interval;
				Reason = reason;
			}
		}

		private class WrappingConfigSnapshot : IConfigSnapshot
		{
			private readonly CentralConfigReader _centralConfig;
			private readonly IConfigSnapshot _wrapped;

			internal WrappingConfigSnapshot(IConfigSnapshot wrapped, CentralConfigReader centralConfig, string dbgDescription)
			{
				_wrapped = wrapped;
				_centralConfig = centralConfig;
				DbgDescription = dbgDescription;
			}

			public string CaptureBody => _centralConfig.CaptureBody ?? _wrapped.CaptureBody;

			public List<string> CaptureBodyContentTypes => _centralConfig.CaptureBodyContentTypes ?? _wrapped.CaptureBodyContentTypes;

			public bool CaptureHeaders => _centralConfig.CaptureHeaders ?? _wrapped.CaptureHeaders;
			public bool CentralConfig => _wrapped.CentralConfig;

			public string DbgDescription { get; }

			public IReadOnlyList<WildcardMatcher> DisableMetrics => _wrapped.DisableMetrics;

			public string Environment => _wrapped.Environment;

			public TimeSpan FlushInterval => _wrapped.FlushInterval;

			public IReadOnlyDictionary<string, string> GlobalLabels => _wrapped.GlobalLabels;
			public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls => _wrapped.TransactionIgnoreUrls;

			public LogLevel LogLevel => _centralConfig.LogLevel ?? _wrapped.LogLevel;

			public int MaxBatchEventCount => _wrapped.MaxBatchEventCount;

			public int MaxQueueEventCount => _wrapped.MaxQueueEventCount;

			public double MetricsIntervalInMilliseconds => _wrapped.MetricsIntervalInMilliseconds;

			public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => _wrapped.SanitizeFieldNames;

			public string SecretToken => _wrapped.SecretToken;
			public string ApiKey => _wrapped.ApiKey;

			public IReadOnlyList<Uri> ServerUrls => _wrapped.ServerUrls;

			public string ServiceName => _wrapped.ServiceName;
			public string ServiceNodeName => _wrapped.ServiceNodeName;

			public string ServiceVersion => _wrapped.ServiceVersion;

			public double SpanFramesMinDurationInMilliseconds =>
				_centralConfig.SpanFramesMinDurationInMilliseconds ?? _wrapped.SpanFramesMinDurationInMilliseconds;

			public int StackTraceLimit => _centralConfig.StackTraceLimit ?? _wrapped.StackTraceLimit;

			public int TransactionMaxSpans => _centralConfig.TransactionMaxSpans ?? _wrapped.TransactionMaxSpans;

			public double TransactionSampleRate => _centralConfig.TransactionSampleRate ?? _wrapped.TransactionSampleRate;

			public bool UseElasticTraceparentHeader => _wrapped.UseElasticTraceparentHeader;

			public bool VerifyServerCert => _wrapped.VerifyServerCert;
			public IReadOnlyCollection<string> ExcludedNamespaces => _wrapped.ExcludedNamespaces;
			public IReadOnlyCollection<string> ApplicationNamespaces => _wrapped.ApplicationNamespaces;
		}
	}
}
