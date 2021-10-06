// Licensed to Elasticsearch B.V under
// one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;

namespace Elastic.Apm.BackendComm.CentralConfig
{
	internal class CentralConfigurationFetcher : BackendCommComponentBase, ICentralConfigurationFetcher
	{
		private const string ThisClassName = nameof(CentralConfigurationFetcher);

		internal static readonly TimeSpan GetConfigHttpRequestTimeout = TimeSpan.FromMinutes(5);
		internal static readonly TimeSpan WaitTimeIfAnyError = TimeSpan.FromMinutes(5);

		private readonly IAgentTimer _agentTimer;
		private readonly ICentralConfigurationResponseParser _centralConfigurationResponseParser;
		private readonly IConfigurationStore _configurationStore;
		private readonly Uri _getConfigAbsoluteUrl;
		private readonly IConfiguration _initialSnapshot;
		private readonly IApmLogger _logger;
		private readonly Action<CentralConfigurationReader> _onResponse;

		private long _dbgIterationsCount;
		private EntityTagHeaderValue _eTag;

		internal CentralConfigurationFetcher(IApmLogger logger, IConfigurationStore configurationStore, ICentralConfigurationResponseParser centralConfigurationResponseParser,
			Service service,
			HttpMessageHandler httpMessageHandler = null, IAgentTimer agentTimer = null, string dbgName = null
		) : this(logger, configurationStore, configurationStore.CurrentSnapshot, service, httpMessageHandler, agentTimer, dbgName) =>
			_centralConfigurationResponseParser = centralConfigurationResponseParser;

		internal CentralConfigurationFetcher(IApmLogger logger, IConfigurationStore configurationStore, Service service
			, HttpMessageHandler httpMessageHandler = null, IAgentTimer agentTimer = null, string dbgName = null
		)
			: this(logger, configurationStore, new CentralConfigurationResponseParser(logger), service, httpMessageHandler, agentTimer, dbgName) { }

		/// <summary>
		/// We need this private ctor to avoid calling configStore.CurrentSnapshot twice (and thus possibly using different
		/// snapshots)
		/// when passing isEnabled: initialConfigSnapshot.CentralConfig and config: initialConfigSnapshot to base
		/// </summary>
		private CentralConfigurationFetcher(IApmLogger logger, IConfigurationStore configurationStore, IConfiguration initialConfiguration, Service service
			, HttpMessageHandler httpMessageHandler, IAgentTimer agentTimer, string dbgName
		)
			: base( /* isEnabled: */ initialConfiguration.CentralConfig, logger, ThisClassName, service, initialConfiguration, httpMessageHandler)
		{
			_logger = logger?.Scoped(ThisClassName + (dbgName == null ? "" : $" (dbgName: `{dbgName}')"));

			_initialSnapshot = initialConfiguration;

			var isCentralConfigOptEqDefault = _initialSnapshot.CentralConfig == ConfigConsts.DefaultValues.CentralConfig;
			var centralConfigStatus = _initialSnapshot.CentralConfig ? "enabled" : "disabled";
			if (!isCentralConfigOptEqDefault) centralConfigStatus = centralConfigStatus.ToUpper();
			_logger.IfLevel(isCentralConfigOptEqDefault ? LogLevel.Debug : LogLevel.Information)
				?.Log("Central configuration feature is {CentralConfigStatus} because CentralConfig option's value is {CentralConfigOptionValue}"
					+ " (default value is {CentralConfigOptionDefaultValue})"
					, centralConfigStatus, _initialSnapshot.CentralConfig, ConfigConsts.DefaultValues.CentralConfig);

			// if the logger supports switching the log level at runtime, allow it to be updated by central configuration
			if (_initialSnapshot.CentralConfig && logger is ILogLevelSwitchable switchable)
				_onResponse += reader =>
				{
					var currentLevel = switchable.LogLevelSwitch.Level;
					if (reader.LogLevel != null && reader.LogLevel != currentLevel)
						switchable.LogLevelSwitch.Level = reader.LogLevel.Value;
				};

			if (!_initialSnapshot.CentralConfig) return;

			_configurationStore = configurationStore;

			_agentTimer = agentTimer ?? new AgentTimer();

			_getConfigAbsoluteUrl = BackendCommUtils.ApmServerEndpoints.BuildGetConfigAbsoluteUrl(initialConfiguration.ServerUrl, service);
			_logger.Debug()
				?.Log("Combined absolute URL for APM Server get central configuration endpoint: `{Url}'. Service: {Service}."
					, _getConfigAbsoluteUrl.Sanitize(), service);

			StartWorkLoop();
		}

		protected override async Task WorkLoopIteration()
		{
			++_dbgIterationsCount;
			WaitInfoS waitInfo;
			HttpRequestMessage httpRequest = null;
			HttpResponseMessage httpResponse = null;
			string httpResponseBody = null;
			try
			{
				httpRequest = BuildHttpRequest(_eTag);

				(httpResponse, httpResponseBody) = await FetchConfigHttpResponseAsync(httpRequest).ConfigureAwait(false);

				CentralConfigurationReader centralConfigurationReader;
				(centralConfigurationReader, waitInfo) = _centralConfigurationResponseParser.ParseHttpResponse(httpResponse, httpResponseBody);
				if (centralConfigurationReader != null)
				{
					_onResponse?.Invoke(centralConfigurationReader);
					UpdateConfigStore(centralConfigurationReader);
					_eTag = httpResponse.Headers.ETag;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				var level = LogLevel.Error;

				if (ex is FailedToFetchConfigException fetchConfigException)
				{
					waitInfo = fetchConfigException.WaitInfo;
					level = fetchConfigException.Severity;
				}
				else
				{
					waitInfo = new WaitInfoS(WaitTimeIfAnyError, "Default wait time is used because exception was thrown"
							+ " while fetching configuration from APM Server and parsing it.");
				}

				_logger.IfLevel(level)?.LogException(ex, "Exception was thrown while fetching configuration from APM Server and parsing it."
						+ " ETag: `{ETag}'. URL: `{Url}'. Apm Server base URL: `{ApmServerUrl}'. WaitInterval: {WaitInterval}."
						+ " dbgIterationsCount: {dbgIterationsCount}."
						+ Environment.NewLine + "+-> Request:{HttpRequest}"
						+ Environment.NewLine + "+-> Response:{HttpResponse}"
						+ Environment.NewLine + "+-> Response body [length: {HttpResponseBodyLength}]:{HttpResponseBody}"
						, _eTag.AsNullableToString()
						, _getConfigAbsoluteUrl.Sanitize().ToString()
						, HttpClient.BaseAddress.Sanitize().ToString()
						, waitInfo.Interval.ToHms(),
						_dbgIterationsCount
						, httpRequest == null ? " N/A" : Environment.NewLine + httpRequest.Sanitize(_configurationStore.CurrentSnapshot.SanitizeFieldNames).ToString().Indent()
						, httpResponse == null ? " N/A" : Environment.NewLine + httpResponse.ToString().Indent()
						, httpResponseBody == null ? "N/A" : httpResponseBody.Length.ToString()
						, httpResponseBody == null ? " N/A" : Environment.NewLine + httpResponseBody.Indent());
			}
			finally
			{
				httpRequest?.Dispose();
				httpResponse?.Dispose();
			}

			_logger.Trace()?.Log("Waiting {WaitInterval}... {WaitReason}. dbgIterationsCount: {dbgIterationsCount}."
					, waitInfo.Interval.ToHms(), waitInfo.Reason, _dbgIterationsCount);
			await _agentTimer.Delay(_agentTimer.Now + waitInfo.Interval, CancellationTokenSource.Token).ConfigureAwait(false);
		}

		private HttpRequestMessage BuildHttpRequest(EntityTagHeaderValue eTag)
		{
			var httpRequest = new HttpRequestMessage(HttpMethod.Get, _getConfigAbsoluteUrl);
			if (eTag != null) httpRequest.Headers.IfNoneMatch.Add(eTag);
			return httpRequest;
		}

		private async Task<(HttpResponseMessage, string)> FetchConfigHttpResponseImplAsync(HttpRequestMessage httpRequest)
		{
			_logger.Trace()?.Log("Making HTTP request to APM Server... Request: {HttpRequest}.", httpRequest.RequestUri.Sanitize());

			var httpResponse = await HttpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseContentRead, CancellationTokenSource.Token)
				.ConfigureAwait(false);
			// ReSharper disable once InvertIf
			if (httpResponse == null)
			{
				throw new FailedToFetchConfigException("HTTP client API call for request to APM Server returned null."
					+ $" Request:{Environment.NewLine}{httpRequest.Sanitize(_configurationStore.CurrentSnapshot.SanitizeFieldNames).ToString().Indent()}",
					new WaitInfoS(WaitTimeIfAnyError, "HttpResponseMessage from APM Server is null"));
			}

			_logger.Trace()?.Log("Reading HTTP response body... Response: {HttpResponse}.", httpResponse);
			var httpResponseBody = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

			return (httpResponse, httpResponseBody);
		}

		private async Task<(HttpResponseMessage, string)> FetchConfigHttpResponseAsync(HttpRequestMessage httpRequest) =>
			await _agentTimer.AwaitOrTimeout(FetchConfigHttpResponseImplAsync(httpRequest)
				, _agentTimer.Now + GetConfigHttpRequestTimeout, CancellationTokenSource.Token).ConfigureAwait(false);

		private void UpdateConfigStore(CentralConfigurationReader centralConfigurationReader)
		{
			_logger.Info()?.Log("Updating " + nameof(ConfigurationStore) + ". New central configuration: {CentralConfiguration}", centralConfigurationReader);

			_configurationStore.CurrentSnapshot = new WrappingConfiguration(_initialSnapshot, centralConfigurationReader
				, $"{_initialSnapshot.Description()} + central (ETag: `{centralConfigurationReader.ETag}')");
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

		private class WrappingConfiguration : IConfiguration, IConfigurationSnapshotDescription
		{
			private readonly CentralConfigurationReader _centralConfiguration;
			private readonly IConfiguration _wrapped;

			internal WrappingConfiguration(IConfiguration wrapped, CentralConfigurationReader centralConfiguration, string description)
			{
				_wrapped = wrapped;
				_centralConfiguration = centralConfiguration;
				Description = description;
			}

			public string ApiKey => _wrapped.ApiKey;
			public IReadOnlyCollection<string> ApplicationNamespaces => _wrapped.ApplicationNamespaces;

			public string CaptureBody => _centralConfiguration.CaptureBody ?? _wrapped.CaptureBody;

			public List<string> CaptureBodyContentTypes => _centralConfiguration.CaptureBodyContentTypes ?? _wrapped.CaptureBodyContentTypes;

			public bool CaptureHeaders => _centralConfiguration.CaptureHeaders ?? _wrapped.CaptureHeaders;
			public bool CentralConfig => _wrapped.CentralConfig;

			public string CloudProvider => _wrapped.CloudProvider;

			public string Description { get; }

			public IReadOnlyList<WildcardMatcher> DisableMetrics => _wrapped.DisableMetrics;
			public bool Enabled => _wrapped.Enabled;

			public string Environment => _wrapped.Environment;
			public IReadOnlyCollection<string> ExcludedNamespaces => _wrapped.ExcludedNamespaces;

			public TimeSpan FlushInterval => _wrapped.FlushInterval;

			public IReadOnlyDictionary<string, string> GlobalLabels => _wrapped.GlobalLabels;

			public string HostName => _wrapped.HostName;

			public IReadOnlyList<WildcardMatcher> IgnoreMessageQueues => _centralConfiguration.IgnoreMessageQueues ?? _wrapped.IgnoreMessageQueues;

			public LogLevel LogLevel => _centralConfiguration.LogLevel ?? _wrapped.LogLevel;

			public int MaxBatchEventCount => _wrapped.MaxBatchEventCount;

			public int MaxQueueEventCount => _wrapped.MaxQueueEventCount;

			public double MetricsIntervalInMilliseconds => _wrapped.MetricsIntervalInMilliseconds;
			public bool Recording => _centralConfiguration.Recording ?? _wrapped.Recording;

			public IReadOnlyList<WildcardMatcher> SanitizeFieldNames => _centralConfiguration.SanitizeFieldNames ?? _wrapped.SanitizeFieldNames;

			public string SecretToken => _wrapped.SecretToken;

			public string ServerCert => _wrapped.ServerCert;

			public Uri ServerUrl => _wrapped.ServerUrl;

			[Obsolete("Use ServerUrl")]
			public IReadOnlyList<Uri> ServerUrls => _wrapped.ServerUrls;

			public string ServiceName => _wrapped.ServiceName;
			public string ServiceNodeName => _wrapped.ServiceNodeName;

			public string ServiceVersion => _wrapped.ServiceVersion;

			public double SpanFramesMinDurationInMilliseconds =>
				_centralConfiguration.SpanFramesMinDurationInMilliseconds ?? _wrapped.SpanFramesMinDurationInMilliseconds;

			public int StackTraceLimit => _centralConfiguration.StackTraceLimit ?? _wrapped.StackTraceLimit;
			public IReadOnlyList<WildcardMatcher> TransactionIgnoreUrls => _centralConfiguration.TransactionIgnoreUrls ?? _wrapped.TransactionIgnoreUrls;

			public int TransactionMaxSpans => _centralConfiguration.TransactionMaxSpans ?? _wrapped.TransactionMaxSpans;

			public double TransactionSampleRate => _centralConfiguration.TransactionSampleRate ?? _wrapped.TransactionSampleRate;

			public bool UseElasticTraceparentHeader => _wrapped.UseElasticTraceparentHeader;

			public bool VerifyServerCert => _wrapped.VerifyServerCert;
			public bool TraceContextIgnoreSampledFalse => _wrapped.TraceContextIgnoreSampledFalse;
		}
	}
}
