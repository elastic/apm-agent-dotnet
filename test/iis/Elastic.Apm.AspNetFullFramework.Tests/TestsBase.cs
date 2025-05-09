// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.Utilities;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using static Elastic.Apm.Config.ConfigurationOption;
using Environment = System.Environment;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public class TestsBase : LoggingTestBase, IAsyncLifetime
	{
		private const string ThisClassName = nameof(AspNetFullFramework) + "." + nameof(Tests) + "." + nameof(TestsBase);

		private static readonly string TearDownPersistentDataReason;

		private static readonly bool TearDownPersistentData =
			EnvVarUtils.GetBoolValue("ELASTIC_APM_TESTS_FULL_FRAMEWORK_TEAR_DOWN_PERSISTENT_DATA", /* defaultValue: */ true,
				out TearDownPersistentDataReason);


		protected readonly AgentConfiguration AgentConfig = new AgentConfiguration();
		protected readonly Dictionary<string, string> EnvVarsToSetForSampleAppPool;

		protected readonly HttpClient HttpClient;
		protected readonly MockApmServer MockApmServer;
		protected readonly bool SampleAppShouldHaveAccessToPerfCounters;
		private readonly IisAdministration _iisAdministration;

		private readonly IApmLogger _logger;
		private readonly int _mockApmServerPort;
		private readonly bool _sampleAppLogEnabled;
		private readonly string _sampleAppLogFilePath;
		private readonly bool _startMockApmServer;
		private readonly DateTime _testStartTime = DateTime.UtcNow;

		protected TestsBase(
			ITestOutputHelper xUnitOutputHelper,
			bool startMockApmServer = true,
			IDictionary<string, string> envVarsToSetForSampleAppPool = null,
			bool sampleAppShouldHaveAccessToPerfCounters = false,
			bool sampleAppLogEnabled = true
		) : base(xUnitOutputHelper)
		{
			_logger = LoggerBase.Scoped(ThisClassName);

			MockApmServer = new MockApmServer(_logger, TestDisplayName);
			_iisAdministration = new IisAdministration(_logger);
			_startMockApmServer = startMockApmServer;
			SampleAppShouldHaveAccessToPerfCounters = sampleAppShouldHaveAccessToPerfCounters;

			_mockApmServerPort = _startMockApmServer ? MockApmServer.FindAvailablePortToListen() : ConfigConsts.DefaultValues.ApmServerPort;

			_sampleAppLogEnabled = sampleAppLogEnabled;
			_sampleAppLogFilePath = GetSampleAppLogFilePath();
			HttpClient = new HttpClient();

			EnvVarsToSetForSampleAppPool = envVarsToSetForSampleAppPool == null
				? new Dictionary<string, string>()
				: new Dictionary<string, string>(envVarsToSetForSampleAppPool);
			EnvVarsToSetForSampleAppPool.TryAdd(ServerUrls.ToEnvironmentVariable(), BuildApmServerUrl(_mockApmServerPort));
			EnvVarsToSetForSampleAppPool.TryAdd(SpanCompressionEnabled.ToEnvironmentVariable(), "false");

			if (_sampleAppLogEnabled)
				EnvVarsToSetForSampleAppPool.TryAdd(LoggingConfig.LogFileEnvVarName, _sampleAppLogFilePath);

			EnvVarsToSetForSampleAppPool.TryAdd(FlushInterval.ToEnvironmentVariable(), "10ms");
		}

		private static class DataSentByAgentVerificationConsts
		{
			internal const int LogMessageAfterNInitialAttempts = 30; // i.e., log the first message after 3 seconds (if it's still failing)
			internal const int LogMessageEveryNAttempts = 10; // i.e., log message every second (if it's still failing)
			internal const int MaxNumberOfAttemptsToVerify = 100;
			internal const int WaitBetweenVerifyAttemptsMs = 100;
		}

		internal static class SampleAppUrlPaths
		{
			internal static readonly SampleAppUrlPathData AboutPage =
				new SampleAppUrlPathData(HomeController.AboutPageRelativePath, 200);

			/// <summary>
			/// Contact page processing does HTTP Get for About page (additional transaction) and https://elastic.co/ - so 2 spans
			/// </summary>
			internal static readonly SampleAppUrlPathData ContactPage =
				new SampleAppUrlPathData(HomeController.ContactPageRelativePath, 200, /* transactionsCount: */ 2, /* spansCount: */ 2);

			/// <summary>
			/// errorsCount is 2 because the exception is thrown inside a span -
			/// AspNetFullFrameworkSampleApp.Controllers.HomeController.CustomSpanThrowsInternal
			/// and exception propagates out of transaction as well so both span and transaction send an error event
			/// </summary>
			internal static readonly SampleAppUrlPathData CustomSpanThrowsExceptionPage =
				new SampleAppUrlPathData(HomeController.CustomSpanThrowsPageRelativePath, 500, spansCount: 1, errorsCount: 2,
					outcome: Outcome.Failure);

			internal static readonly SampleAppUrlPathData HomePage =
				new SampleAppUrlPathData(HomeController.HomePageRelativePath, 200);

			internal static readonly SampleAppUrlPathData CookiesPage =
				new SampleAppUrlPathData(HomeController.CookiesPageRelativePath, 200);

			internal static readonly SampleAppUrlPathData PageThatDoesNotExist =
				new SampleAppUrlPathData("dummy_URL_path_to_page_that_does_not_exist", 404, errorsCount: 1);

			internal static readonly List<SampleAppUrlPathData> AllPaths = new List<SampleAppUrlPathData>
			{
				new SampleAppUrlPathData("", 200),
				HomePage,
				ContactPage,
				CustomSpanThrowsExceptionPage,
				PageThatDoesNotExist
			};

			/// <summary>
			/// `CallReturnBadRequest' page processing does HTTP Get for `ReturnBadRequest' page (additional transaction) - so 1 span
			/// </summary>
			internal static readonly SampleAppUrlPathData CallReturnBadRequestPage =
				new SampleAppUrlPathData(HomeController.CallReturnBadRequestPageRelativePath,
					HomeController.DummyHttpStatusCode, /* transactionsCount: */ 2, /* spansCount: */ 1);

			internal static readonly SampleAppUrlPathData CallSoapServiceProtocolV11 =
				new SampleAppUrlPathData("Asmx/Health.asmx", (int)HttpStatusCode.OK);

			internal static readonly SampleAppUrlPathData CallSoapServiceProtocolV12 =
				new SampleAppUrlPathData("Asmx/Health.asmx", (int)HttpStatusCode.OK);

			internal static readonly SampleAppUrlPathData ChildHttpSpanWithResponseForbiddenPage =
				new SampleAppUrlPathData(HomeController.ChildHttpSpanWithResponseForbiddenPath, 200, transactionsCount: 2, spansCount: 1, errorsCount: 0);


			/// <summary>
			/// errorsCount is 3 because the exception is thrown inside a child span of a another span -
			/// AspNetFullFrameworkSampleApp.Controllers.HomeController.CustomSpanThrowsInternal - child span
			/// AspNetFullFrameworkSampleApp.Controllers.HomeController.CustomChildSpanThrows - transaction and contains parent span
			/// and exception propagates out of transaction as well so both spans and the transaction send an error event
			/// </summary>
			internal static readonly SampleAppUrlPathData CustomChildSpanThrowsExceptionPage =
				new SampleAppUrlPathData(HomeController.CustomChildSpanThrowsPageRelativePath, 500, spansCount: 2, errorsCount: 3,
					outcome: Outcome.Failure);


			internal static readonly SampleAppUrlPathData GenNSpansPage =
				new SampleAppUrlPathData(HomeController.GenNSpansPageRelativePath, (int)HttpStatusCode.Created);

			internal static readonly SampleAppUrlPathData GetDotNetRuntimeDescriptionPage =
				new SampleAppUrlPathData(HomeController.GetDotNetRuntimeDescriptionPageRelativePath, 200);


			internal static readonly SampleAppUrlPathData LabelsTest =
				new SampleAppUrlPathData(HomeController.LabelsTestRelativePath, 200, 1, 1);

			internal static readonly SampleAppUrlPathData MyAreaHomePage =
				new SampleAppUrlPathData(AspNetFullFrameworkSampleApp.Areas.MyArea.Controllers.HomeController.HomePageRelativePath, 200);

			internal static readonly SampleAppUrlPathData NotFoundPage =
				new SampleAppUrlPathData(HomeController.NotFoundPageRelativePath, 404, errorsCount: 1);

			internal static readonly SampleAppUrlPathData ReturnBadRequestPage =
				new SampleAppUrlPathData(HomeController.ReturnBadRequestPageRelativePath, (int)HttpStatusCode.BadRequest);

			internal static readonly SampleAppUrlPathData RoutedWebformsPage =
				new SampleAppUrlPathData(nameof(Webforms.RoutedWebforms), 200);

			internal static readonly SampleAppUrlPathData ThrowsHttpException404PageRelativePath =
				new SampleAppUrlPathData(HomeController.ThrowsHttpException404PageRelativePath, 404, errorsCount: 1, outcome: Outcome.Failure);

			internal static readonly SampleAppUrlPathData ThrowsInvalidOperationPage =
				new SampleAppUrlPathData(HomeController.ThrowsInvalidOperationPageRelativePath, 500, errorsCount: 1, outcome: Outcome.Failure);

			internal static readonly SampleAppUrlPathData WebApiPage =
				new SampleAppUrlPathData(WebApiController.Path, 200);

			internal static SampleAppUrlPathData AttributeRoutingWebApiPage(string id) =>
				new SampleAppUrlPathData(AttributeRoutingWebApiController.RoutePrefix + "/" + id, 200);

			internal static readonly SampleAppUrlPathData WebformsPage =
				new SampleAppUrlPathData(nameof(Webforms) + ".aspx", 200);

			internal static readonly SampleAppUrlPathData WebformsExceptionPage =
				new SampleAppUrlPathData(nameof(WebformsException) + ".aspx", 500, errorsCount: 1, outcome: Outcome.Failure);
		}

		private TimedEvent? _sampleAppClientCallTiming;

		public Task InitializeAsync()
		{
			// Mock APM server should be started only after sample application is started in clean state.
			// The order is important to prevent agent's queued data from the previous test to be sent
			// to this test instance of mock APM server.
			_iisAdministration.SetupSampleAppInCleanState(EnvVarsToSetForSampleAppPool, SampleAppShouldHaveAccessToPerfCounters);
			if (_startMockApmServer)
				MockApmServer.RunInBackground(_mockApmServerPort);
			else
			{
				_logger.Info()
					?.Log("Not starting mock APM server because startMockApmServer argument to ctor is {startMockApmServer}", _startMockApmServer);
			}

			return Task.CompletedTask;
		}

		public async Task DisposeAsync()
		{
			if (TearDownPersistentData)
				_iisAdministration.DisposeSampleApp();
			else
			{
				_logger.Warning()
					?.Log("Not tearing down IIS sample application and pool because {Reason}", TearDownPersistentDataReason);
			}

			if (_startMockApmServer)
				await MockApmServer.StopAsync();

			_logger.Info()?.Log("Finished test: {FullUnitTestName}", TestDisplayName);
		}

		private string GetSampleAppLogFilePath()
		{
			var sampleAppLogFilePath = Environment.GetEnvironmentVariable(LoggingConfig.LogFileEnvVarName);
			if (sampleAppLogFilePath != null)
			{
				_logger.Info()
					?.Log("Environment variable `{SampleAppLogFileEnvVarName}' is set to `{SampleAppLogFilePath}'"
						+ " - using it to write/read sample application's and agent's log", LoggingConfig.LogFileEnvVarName, sampleAppLogFilePath);
				return sampleAppLogFilePath;
			}

			sampleAppLogFilePath = Path.Combine(Path.GetTempPath(), $"{Consts.SampleApp.AppName}.log");
			_logger.Info()
				?.Log("Environment variable `{SampleAppLogFileEnvVarName}' is not set"
					+ " - using `{SampleAppLogFilePath}' to write/read sample application's and agent's log",
					LoggingConfig.LogFileEnvVarName, sampleAppLogFilePath);
			return sampleAppLogFilePath;
		}

		private static string BuildApmServerUrl(int apmServerPort) => $"http://localhost:{apmServerPort}/";

		protected Task<SampleAppResponse> SendGetRequestToSampleAppAndVerifyResponse(Uri uri, int expectedStatusCode,
			bool timeHttpCall = true, bool addTraceContextHeaders = false, HttpContent httpContent = null)
				=> SendRequestToSampleAppAndVerifyResponse(HttpMethod.Get, uri, expectedStatusCode, timeHttpCall, addTraceContextHeaders, httpContent);

		protected Task<SampleAppResponse> SendPostRequestToSampleAppAndVerifyResponse(Uri uri, int expectedStatusCode,
			bool timeHttpCall = true, bool addTraceContextHeaders = false, HttpContent httpContent = null)
				=> SendRequestToSampleAppAndVerifyResponse(HttpMethod.Post, uri, expectedStatusCode, timeHttpCall, addTraceContextHeaders, httpContent);

		protected async Task<SampleAppResponse> SendRequestToSampleAppAndVerifyResponse(HttpMethod httpMethod, Uri uri, int expectedStatusCode,
			bool timeHttpCall = true, bool addTraceContextHeaders = false, HttpContent httpContent = null)
		{
			var startTime = DateTime.UtcNow;
			if (timeHttpCall)
			{
				_logger.Debug()
					?.Log("HTTP call to sample application started at {Time} (as timestamp: {Timestamp})",
						startTime, TimeUtils.ToTimestamp(startTime));
			}
			try
			{
				var httpRequestMessage = new HttpRequestMessage(httpMethod, uri)
				{
					Content = httpContent
				};

				if (addTraceContextHeaders)
				{
					httpRequestMessage.Headers.Add("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
					httpRequestMessage.Headers.Add("tracestate", "rojo=00f067aa0ba902b7,congo=t61rcWkgMzE");
				}

				var response = await SendGetRequestToSampleAppAndVerifyResponseImpl(httpRequestMessage, expectedStatusCode);
				return new SampleAppResponse(response.Headers, await response.Content.ReadAsStringAsync());
			}
			finally
			{
				if (timeHttpCall)
				{
					_sampleAppClientCallTiming.Should().BeNull();
					var endTime = DateTime.UtcNow;
					_logger.Debug()
						?.Log("HTTP call to sample application ended at {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
							endTime, TimeUtils.ToTimestamp(endTime),
							TimeUtils.DurationBetweenTimestamps(TimeUtils.ToTimestamp(startTime), TimeUtils.ToTimestamp(endTime)));
					_sampleAppClientCallTiming = new TimedEvent(startTime, endTime);
				}
			}
		}

		private async Task<HttpResponseMessage> SendGetRequestToSampleAppAndVerifyResponseImpl(
			HttpRequestMessage httpRequestMessage,
			int expectedStatusCode
		)
		{
			var url = httpRequestMessage.RequestUri;
			_logger.Debug()?.Log("Sending request with URL: {url} and expected status code: {HttpStatusCode}...", url, expectedStatusCode);
			var response = await HttpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
			_logger.Debug()
				?.Log("Request sent. Actual status code: {HttpStatusCode} ({HttpStatusCodeEnum})",
					(int)response.StatusCode, response.StatusCode);
			try
			{
				response.StatusCode.Should().Be((HttpStatusCode)expectedStatusCode);
			}
			catch (XunitException ex)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				_logger.Error()?.Log("{ExceptionMessage}. Response content:\n{ResponseContent}", ex.Message, responseContent);
				throw;
			}

			var processIdInResponse = response.Headers.GetValues(AspNetFullFrameworkSampleApp.Consts.ProcessIdResponseHeaderName);
			_logger.Debug()
				?.Log("{ProcessIdHeaderName} in response is {ProcessIdHeaderValue}",
					AspNetFullFrameworkSampleApp.Consts.ProcessIdResponseHeaderName, processIdInResponse);

			var apmServerUrlsInResponse =
				response.Headers.GetValues(AspNetFullFrameworkSampleApp.Consts.ElasticApmServerUrlsResponseHeaderName).ToList();
			try
			{
				apmServerUrlsInResponse.Should().HaveCount(1);
				apmServerUrlsInResponse.First().Should().Be(BuildApmServerUrl(_mockApmServerPort));
			}
			catch (XunitException ex)
			{
				_logger.Error()
					?.LogException(ex, "Sample application's APM-server-URLs configuration setting ({ActualApmServerUrl})" +
						" is different from expected ({ExpectedApmServerUrl})",
						string.Join(", ", apmServerUrlsInResponse), BuildApmServerUrl(_mockApmServerPort));

				await PostTestFailureDiagnostics();

				throw;
			}

			return response;
		}

		protected async Task WaitAndCustomVerifyReceivedData(Action<ReceivedData> verifyAction, bool shouldGatherDiagnostics = true)
		{
			var attemptNumber = 0;
			var timerSinceStart = Stopwatch.StartNew();
			while (true)
			{
				++attemptNumber;

				if (!MockApmServer.ReceivedData.InvalidPayloadErrors.IsEmpty)
				{
					var messageBuilder = new StringBuilder();
					messageBuilder.AppendLine("There is at least one invalid payload error - the test is considered as failed.");
					messageBuilder.AppendLine("Invalid payload error(s):".Indent(1));
					foreach (var invalidPayloadError in MockApmServer.ReceivedData.InvalidPayloadErrors)
						messageBuilder.AppendLine(invalidPayloadError.Indent(2));
					throw new XunitException(messageBuilder.ToString());
				}

				try
				{
					verifyAction(MockApmServer.ReceivedData);
					timerSinceStart.Stop();
					_logger.Debug()
						?.Log("Data received from agent passed verification." +
							" Time elapsed: {VerificationTimeSeconds}s." +
							" Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}",
							timerSinceStart.Elapsed.TotalSeconds,
							attemptNumber, DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify);
					if (shouldGatherDiagnostics)
					{
						LogSampleAppLogFileContent();
						await LogSampleAppDiagnosticsPage();
					}
					return;
				}
				catch (XunitException ex)
				{
					var logOnThisAttempt =
						attemptNumber >= DataSentByAgentVerificationConsts.LogMessageAfterNInitialAttempts &&
						attemptNumber % DataSentByAgentVerificationConsts.LogMessageEveryNAttempts == 0;

					if (logOnThisAttempt)
					{
						_logger.Warning()
							?.LogException(ex,
								"Data received from agent did NOT pass verification." +
								" Time elapsed: {VerificationTimeSeconds}s." +
								" Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}" +
								" This message is printed only every {LogMessageEveryNAttempts} attempts",
								timerSinceStart.Elapsed.TotalSeconds,
								attemptNumber, DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify,
								DataSentByAgentVerificationConsts.LogMessageEveryNAttempts);
					}

					if (attemptNumber == DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify)
					{
						_logger.Error()?.LogException(ex, "Reached max number of attempts to verify payload - Rethrowing the last exception...");
						await PostTestFailureDiagnostics();
						throw;
					}

					if (logOnThisAttempt)
					{
						_logger.Debug()
							?.Log("Waiting {WaitBetweenVerifyAttemptsMs}ms before the next attempt..." +
								" This message is printed only every {LogMessageEveryNAttempts} attempts",
								DataSentByAgentVerificationConsts.WaitBetweenVerifyAttemptsMs,
								DataSentByAgentVerificationConsts.LogMessageEveryNAttempts);
					}

					Thread.Sleep(DataSentByAgentVerificationConsts.WaitBetweenVerifyAttemptsMs);
				}
				catch (Exception ex)
				{
					_logger.Error()?.LogException(ex, "Exception escaped from verifier");
					throw;
				}
			}
		}

		// ReSharper disable once MemberCanBeProtected.Global
		public static IEnumerable<object[]> AllSampleAppUrlPaths() => SampleAppUrlPaths.AllPaths.Select(data => new object[] { data });

		public static SampleAppUrlPathData RandomSampleAppUrlPath() =>
			SampleAppUrlPaths.AllPaths[RandomGenerator.GetInstance().Next(0, SampleAppUrlPaths.AllPaths.Count)];

		private void LogSampleAppLogFileContent()
		{
			if (!_sampleAppLogEnabled)
			{
				_logger.Info()?.Log("Sample application log is disabled");
				return;
			}

			string sampleAppLogFileContent;
			try
			{
				sampleAppLogFileContent = File.ReadAllText(_sampleAppLogFilePath);
			}
			catch (Exception ex)
			{
				_logger.Info()
					?.LogException(ex, "Exception thrown while trying to read sample application log file (`{SampleAppLogFilePath}')",
						_sampleAppLogFilePath);
				return;
			}

			_logger.Info()?.Log("Sample application log:\n{SampleAppLogFileContent}", TextUtils.Indent(sampleAppLogFileContent));
		}

		private async Task LogSampleAppDiagnosticsPage()
		{
			var url = new SampleAppUri(DiagnosticsController.DiagnosticsPageRelativePath);
			_logger.Debug()?.Log("Getting content of sample application diagnostics page ({url})...", url);
			var response = await HttpClient.GetAsync(url);
			_logger.Debug()
				?.Log("Received sample application's diagnostics page. Status code: {HttpStatusCode} ({HttpStatusCodeEnum})",
					(int)response.StatusCode, response.StatusCode);

			_logger.Info()
				?.Log("Sample application's diagnostics page content:\n{DiagnosticsPageContent}",
					TextUtils.Indent(await response.Content.ReadAsStringAsync()));
		}

		private async Task PostTestFailureDiagnostics()
		{
			_iisAdministration.LogIisApplicationHostConfig();
			LogSampleAppLogFileContent();
			await LogSampleAppDiagnosticsPage();

			_logger.Debug()
				?.Log("Analyzing potential issues... _mockApmServer.ReceivedData: " +
					"#transactions: {NumberOfTransactions}, #spans: {NumberOfSpans}, #errors: {NumberOfErrors}, #metric sets: {NumberOfMetricSets}",
					MockApmServer.ReceivedData.Transactions.Count,
					MockApmServer.ReceivedData.Spans.Count,
					MockApmServer.ReceivedData.Errors.Count,
					MockApmServer.ReceivedData.Metrics.Count);

			FindReceivedDataWithTimestampEarlierThanTestStart();
		}

		private void FindReceivedDataWithTimestampEarlierThanTestStart()
		{
			foreach (var error in MockApmServer.ReceivedData.Errors)
				AnalyzeDtoTimestamp(error.Timestamp, error);
			foreach (var metricSet in MockApmServer.ReceivedData.Metrics)
				AnalyzeDtoTimestamp(metricSet.Timestamp, metricSet);
			foreach (var span in MockApmServer.ReceivedData.Spans)
				AnalyzeDtoTimestamp(span.Timestamp, span);
			foreach (var transaction in MockApmServer.ReceivedData.Transactions)
				AnalyzeDtoTimestamp(transaction.Timestamp, transaction);

			void AnalyzeDtoTimestamp(long dtoTimestamp, object dto)
			{
				var dtoStartTime = TimeUtils.ToDateTime(dtoTimestamp);

				if (_testStartTime <= dtoStartTime)
					return;

				_logger.Warning()
					?.Log("The following DTO received from the agent has timestamp that is earlier than the current test start time. " +
						"DTO timestamp: {DtoTimestamp}, test start time: {TestStartTime}, DTO: {DtoFromAgent}",
						dtoStartTime.ToLocalTime().FormatForLog(), _testStartTime.ToLocalTime().FormatForLog(), dto);
			}
		}

		protected async Task WaitAndVerifyReceivedDataSharedConstraints(
			SampleAppUrlPathData sampleAppUrlPathData
			, bool shouldGatherDiagnostics = true
		) =>
			await WaitAndCustomVerifyReceivedData(receivedData => { VerifyReceivedDataSharedConstraints(sampleAppUrlPathData, receivedData); },
				shouldGatherDiagnostics);

		protected void VerifyReceivedDataSharedConstraints(SampleAppUrlPathData sampleAppUrlPathData, ReceivedData receivedData)
		{
			FullFwAssertValid(receivedData);

			receivedData.Transactions.Count.Should().Be(sampleAppUrlPathData.TransactionsCount);
			receivedData.Spans.Count.Should().Be(sampleAppUrlPathData.SpansCount);
			receivedData.Errors.Count.Should().Be(sampleAppUrlPathData.ErrorsCount);

			if (receivedData.Transactions.Count != 1)
				return;

			var transaction = receivedData.Transactions.First();
			if (transaction.Context != null)
			{
				transaction.Context.Request.Url.Full.Should().Be(sampleAppUrlPathData.Uri.AbsoluteUri);
				transaction.Context.Request.Url.PathName.Should().Be(sampleAppUrlPathData.Uri.AbsolutePath);

				if (string.IsNullOrEmpty(sampleAppUrlPathData.Uri.Query))
					transaction.Context.Request.Url.Search.Should().BeNull();
				else
				{
					// Uri.Query always unescapes the querystring so don't use it, and instead get the escaped querystring.
					var queryString = sampleAppUrlPathData.Uri.GetComponents(UriComponents.Query, UriFormat.UriEscaped);
					transaction.Context.Request.Url.Search.Should().Be(queryString);
				}

				transaction.Context.Response.StatusCode.Should().Be(sampleAppUrlPathData.StatusCode);
				transaction.Outcome.Should().Be(sampleAppUrlPathData.Outcome);
			}

			var httpStatusFirstDigit = sampleAppUrlPathData.StatusCode / 100;
			transaction.Result.Should().Be($"HTTP {httpStatusFirstDigit}xx");
			transaction.SpanCount.Started.Should().Be(sampleAppUrlPathData.SpansCount);
		}

		internal void VerifySpanNameTypeSubtypeAction(SpanDto span, string spanPrefix)
		{
			span.Name.Should().Be($"{spanPrefix}{HomeController.SpanNameSuffix}");
			span.Type.Should().Be($"{spanPrefix}{HomeController.SpanTypeSuffix}");
			span.Subtype.Should().Be($"{spanPrefix}{HomeController.SpanSubtypeSuffix}");
			span.Action.Should().Be($"{spanPrefix}{HomeController.SpanActionSuffix}");
		}

		private void FullFwAssertValid(ReceivedData receivedData)
		{
			foreach (var error in receivedData.Errors)
				FullFwAssertValid(error);
			foreach (var metadata in receivedData.Metadata)
				FullFwAssertValid(metadata);
			foreach (var metricSet in receivedData.Metrics)
				FullFwAssertValid(metricSet);
			foreach (var span in receivedData.Spans)
				FullFwAssertValid(span);
			foreach (var transaction in receivedData.Transactions)
				FullFwAssertValid(transaction);
		}

		private void FullFwAssertValid(MetadataDto metadata)
		{
			metadata.Should().NotBeNull();

			if (AgentConfig.GlobalLabels == null)
				metadata.Labels.Should().BeNull();
			else
				metadata.Labels.Should().Equal(AgentConfig.GlobalLabels);

			FullFwAssertValid(metadata.Service);
			FullFwAssertValid(metadata.System);
		}

		private void FullFwAssertValid(Service service)
		{
			service.Should().NotBeNull();

			FullFwAssertValid(service.Framework);

			var expectedServiceName = AgentConfig.ServiceName
				?? AbstractConfigurationReader.AdaptServiceName($"{Consts.SampleApp.SiteName}_{Consts.SampleApp.AppPoolName}");
			var expectedEnvironment = AgentConfig.Environment;
			var expectedServiceNodeName = AgentConfig.ServiceNodeName;

			service.Name.Should().Be(expectedServiceName);
			service.Environment.Should().Be(expectedEnvironment);
			service.Node.ConfiguredName.Should().Be(expectedServiceNodeName);
		}

		private static void FullFwAssertValid(Framework framework)
		{
			framework.Should().NotBeNull();

			framework.Name.Should().Be("ASP.NET");
			framework.Version.Should().StartWith("4.");
		}

		private void FullFwAssertValid(Api.System system)
		{
			system.Should().NotBeNull();

			system.DetectedHostName.Should().Be(new HostNameDetector().GetDetectedHostName(LoggerBase));
#pragma warning disable 618
			system.HostName.Should().Be(AgentConfig.HostName ?? system.DetectedHostName);
#pragma warning restore 618
		}

		private void FullFwAssertValid(ErrorDto error)
		{
			FullFwAssertValid((ITimestampedDto)error);
			error.Transaction.AssertValid();
			if (error.Context != null)
				FullFwAssertValid(error.Context, error);
			error.Culprit.NonEmptyAssertValid();
			error.Exception.AssertValid();
		}

		private void FullFwAssertValid(TransactionDto transaction)
		{
			transaction.Should().NotBeNull();

			if (transaction.Context != null)
				FullFwAssertValid(transaction.Context, transaction);
			transaction.Name.Should().NotBeNull();
			TransactionResultFullFwAssertValid(transaction.Result);
			transaction.Type.Should().Be(ApiConstants.TypeRequest);
			transaction.SpanCount.Should().NotBeNull();
			FullFwAssertValid((ITimedDto)transaction);
		}

		private void FullFwAssertValid(ITimestampedDto timestampedDto)
		{
			timestampedDto.Should().NotBeNull();

			if (_sampleAppClientCallTiming != null)
				timestampedDto.ShouldOccurBetween(_sampleAppClientCallTiming);
		}

		private void FullFwAssertValid(ITimedDto timedDto)
		{
			FullFwAssertValid((ITimestampedDto)timedDto);

			timedDto.Duration.Should().BeGreaterOrEqualTo(0);

			if (_sampleAppClientCallTiming != null)
				timedDto.ShouldOccurBetween(_sampleAppClientCallTiming);
		}

		private static void FullFwAssertValid(Url url)
		{
			url.Should().NotBeNull();

			url.Full.Should().NotBeNull();
			url.Raw.Should().Be(url.Full);
			url.Protocol.Should().Be("HTTP");
			url.HostName.Should().Be(Consts.SampleApp.Host);
			url.PathName.Should().NotBeNull();
		}

		private static void TransactionResultFullFwAssertValid(string result) => result.Should().MatchRegex("HTTP [1-9]xx");

		// ReSharper disable once UnusedParameter.Local
		private void FullFwAssertValid(ContextDto context, TransactionDto _)
		{
			context.Should().NotBeNull();

			FullFwAssertValid(context.Request);
			FullFwAssertValid(context.Response);
		}

		// ReSharper disable once UnusedParameter.Local
		private void FullFwAssertValid(ContextDto context, ErrorDto _)
		{
			context.Should().NotBeNull();

			FullFwAssertValid(context.Request);
		}

		private void FullFwAssertValid(SpanDto span)
		{
			span.Should().NotBeNull();

			FullFwAssertValid((ITimedDto)span);
			if (span.StackTrace != null)
				FullFwAssertValid(span.StackTrace);
		}

		private static void FullFwAssertValid(IEnumerable<CapturedStackFrame> stackTrace)
		{
			// ReSharper disable PossibleMultipleEnumeration
			stackTrace.Should().NotBeNull();

			stackTrace.ForEach(FullFwAssertValid);
			// ReSharper restore PossibleMultipleEnumeration
		}

		private static void FullFwAssertValid(CapturedStackFrame capturedStackFrame)
		{
			capturedStackFrame.Should().NotBeNull();

			capturedStackFrame.Function.NonEmptyAssertValid();
		}

		private void FullFwAssertValid(MetricSetDto metricSet)
		{
			metricSet.Should().NotBeNull();

			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach (var (metricTypeName, _) in metricSet.Samples)
			{
				if (MetricsAssertValid.MetricMetadataPerType[metricTypeName].ImplRequiresAccessToPerfCounters)
				{
					SampleAppShouldHaveAccessToPerfCounters.Should()
						.BeTrue($"Metric {metricTypeName} implementation requires access to performance counters");
				}
			}
		}

		private void FullFwAssertValid(Request request)
		{
			request.Should().NotBeNull();

			FullFwAssertValid(request.Socket);
			FullFwAssertValid(request.Url);

			if (AgentConfig.CaptureHeaders)
			{
				var caseInsensitiveRequestHeaders = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase);
				caseInsensitiveRequestHeaders["Host"].Should().Be(Consts.SampleApp.Host);
			}
			else
				request.Headers.Should().BeNull();
		}

		private static void FullFwAssertValid(Socket socket)
		{
			socket.Should().NotBeNull();
			socket.RemoteAddress.Should().BeOneOf("::1", "127.0.0.1");
		}

		private void FullFwAssertValid(Response response)
		{
			response.Should().NotBeNull();

			if (AgentConfig.CaptureHeaders)
				response.Headers.Should().NotBeNull();
			else
				response.Headers.Should().BeNull();

			response.Finished.Should().BeTrue();
		}

		protected static void ShouldBeMonotonicInTime(IEnumerable<ITimedDto> timedDtos)
		{
			ITimedDto prev = null;
			foreach (var timedDto in timedDtos)
			{
				prev?.ShouldOccurBefore(timedDto);
				prev = timedDto;
			}
		}

		protected static bool OccursBetween(ITimedDto containedDto, ITimedDto containingDto) =>
			containingDto.Timestamp <= containedDto.Timestamp
			&&
			TimeUtils.ToEndDateTime(containedDto.Timestamp, containedDto.Duration)
			<=
			TimeUtils.ToEndDateTime(containingDto.Timestamp, containingDto.Duration);

		protected void ClearState()
		{
			_sampleAppClientCallTiming = null;

			MockApmServer.ClearState();
		}

		protected void AssertReceivedDataSampledStatus(ReceivedData receivedData, bool isSampled, int? expectedSpanCount = null)
		{
			foreach (var transaction in receivedData.Transactions)
			{
				transaction.IsSampled.Should().Be(isSampled);

				if (isSampled)
				{
					transaction.Context.Should().NotBeNull();
					if (expectedSpanCount.HasValue)
						transaction.SpanCount.Started.Should().Be(expectedSpanCount.Value);
				}
				else
				{
					transaction.Context.Should().BeNull();
					transaction.SpanCount.Started.Should().Be(0);
					transaction.SpanCount.Dropped.Should().Be(0);
				}
			}

			foreach (var error in receivedData.Errors)
			{
				error.Transaction.IsSampled.Should().Be(isSampled);

				if (isSampled)
					error.Context.Should().NotBeNull();
				else
					error.Context.Should().BeNull();
			}
		}

		protected struct SampleAppResponse
		{
			internal SampleAppResponse(HttpResponseHeaders headers, string content)
			{
				Headers = headers;
				Content = content;
			}

			internal readonly HttpResponseHeaders Headers;
			internal string Content;
		}

		protected class AgentConfiguration
		{
			internal bool CaptureHeaders = true;
			internal string Environment;
			internal Dictionary<string, string> GlobalLabels;
			internal string HostName;
			internal string ServiceName;
			internal string ServiceNodeName;
		}

		/// <summary>
		/// Uri for the sample application
		/// </summary>
		public class SampleAppUri
		{
			private readonly UriBuilder _builder;

			public SampleAppUri(string relativePathAndQuery) =>
				_builder = new UriBuilder($"{Consts.SampleApp.RootUrl}/{relativePathAndQuery.TrimStart('/')}") { Port = -1 };

			public string RelativePath => _builder.Uri.AbsolutePath.Substring(Consts.SampleApp.RootUrlPath.Length + 1);

			public Uri Uri => _builder.Uri;

			public static implicit operator Uri(SampleAppUri sampleAppUri) => sampleAppUri.Uri;
		}

		public class SampleAppUrlPathData
		{
			public readonly int ErrorsCount;
			public readonly Outcome Outcome;
			public readonly int SpansCount;
			public readonly int StatusCode;
			public readonly int TransactionsCount;
			private readonly string _relativePathAndQuery;
			private readonly SampleAppUri _sampleAppUri;

			public SampleAppUrlPathData(string relativePathAndQuery, int statusCode, int transactionsCount = 1, int spansCount = 0,
				int errorsCount = 0,
				Outcome outcome = Outcome.Success
			)
			{
				_sampleAppUri = new SampleAppUri(relativePathAndQuery);
				_relativePathAndQuery = relativePathAndQuery;
				StatusCode = statusCode;
				TransactionsCount = transactionsCount;
				SpansCount = spansCount;
				ErrorsCount = errorsCount;
				Outcome = outcome;
			}

			public string RelativePath => _sampleAppUri.RelativePath;
			public Uri Uri => _sampleAppUri.Uri;

			public SampleAppUrlPathData Clone(
				string relativePathAndQuery = null,
				int? status = null,
				int? transactionsCount = null,
				int? spansCount = null,
				int? errorsCount = null,
				Outcome outcome = Outcome.Success
			) => new SampleAppUrlPathData(
				relativePathAndQuery ?? _relativePathAndQuery,
				status ?? StatusCode,
				transactionsCount ?? TransactionsCount,
				spansCount ?? SpansCount,
				errorsCount ?? ErrorsCount,
				outcome);
		}
	}
}
