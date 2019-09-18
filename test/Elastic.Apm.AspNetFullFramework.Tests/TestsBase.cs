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
using Elastic.Apm.Model;
using Elastic.Apm.Tests.Extensions;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public class TestsBase : LoggingTestBase, IAsyncLifetime
	{
		private static readonly string TearDownPersistentDataReason;

		private static readonly bool TearDownPersistentData =
			EnvVarUtils.GetBoolValue("ELASTIC_APM_TESTS_FULL_FRAMEWORK_TEAR_DOWN_PERSISTENT_DATA", /* defaultValue: */ true,
				out TearDownPersistentDataReason);


		protected readonly AgentConfiguration AgentConfig = new AgentConfiguration();
		protected readonly bool SampleAppShouldHaveAccessToPerfCounters;
		private readonly Dictionary<string, string> _envVarsToSetForSampleAppPool;
		private readonly IisAdministration _iisAdministration;
		private readonly MockApmServer _mockApmServer;
		private readonly int _mockApmServerPort;
		private readonly bool _sampleAppLogEnabled;
		private readonly string _sampleAppLogFilePath;
		private readonly bool _startMockApmServer;
		private readonly DateTime _testStartTime = DateTime.UtcNow;

		protected TestsBase(ITestOutputHelper xUnitOutputHelper,
			bool startMockApmServer = true,
			IDictionary<string, string> envVarsToSetForSampleAppPool = null,
			bool sampleAppShouldHaveAccessToPerfCounters = false,
			bool sampleAppLogEnabled = true
		) : base(xUnitOutputHelper)
		{
			Logger.Info()?.Log("Starting test: {FullUnitTestName}", TestDisplayName);

			_mockApmServer = new MockApmServer(Logger, TestDisplayName);
			_iisAdministration = new IisAdministration(Logger);
			_startMockApmServer = startMockApmServer;
			SampleAppShouldHaveAccessToPerfCounters = sampleAppShouldHaveAccessToPerfCounters;

			_mockApmServerPort = _startMockApmServer ? _mockApmServer.FindAvailablePortToListen() : ConfigConsts.DefaultValues.ApmServerPort;

			_sampleAppLogEnabled = sampleAppLogEnabled;
			_sampleAppLogFilePath = GetSampleAppLogFilePath();

			_envVarsToSetForSampleAppPool = envVarsToSetForSampleAppPool == null
				? new Dictionary<string, string>()
				: new Dictionary<string, string>(envVarsToSetForSampleAppPool);
			_envVarsToSetForSampleAppPool.TryAdd(ConfigConsts.EnvVarNames.ServerUrls, BuildApmServerUrl(_mockApmServerPort));

			if (_sampleAppLogEnabled) _envVarsToSetForSampleAppPool.TryAdd(LoggingConfig.LogFileEnvVarName, _sampleAppLogFilePath);

			_envVarsToSetForSampleAppPool.TryAdd(ConfigConsts.EnvVarNames.FlushInterval, "10ms");
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

			/// Contact page processing does HTTP Get for About page (additional transaction) and https://elastic.co/ - so 2 spans
			internal static readonly SampleAppUrlPathData ContactPage =
				new SampleAppUrlPathData(HomeController.ContactPageRelativePath, 200, /* transactionsCount: */ 2, /* spansCount: */ 2);

			internal static readonly SampleAppUrlPathData CustomSpanThrowsExceptionPage =
				new SampleAppUrlPathData(HomeController.CustomSpanThrowsPageRelativePath, 500, spansCount: 1, errorsCount: 1);

			internal static readonly SampleAppUrlPathData HomePage =
				new SampleAppUrlPathData(HomeController.HomePageRelativePath, 200);

			internal static readonly List<SampleAppUrlPathData> AllPaths = new List<SampleAppUrlPathData>
			{
				new SampleAppUrlPathData("", 200),
				HomePage,
				ContactPage,
				CustomSpanThrowsExceptionPage,
				new SampleAppUrlPathData("Dummy_nonexistent_path", 404)
			};

			/// `CallReturnBadRequest' page processing does HTTP Get for `ReturnBadRequest' page (additional transaction) - so 1 span
			internal static readonly SampleAppUrlPathData CallReturnBadRequestPage =
				new SampleAppUrlPathData(HomeController.CallReturnBadRequestPageRelativePath,
					HomeController.DummyHttpStatusCode, /* transactionsCount: */ 2, /* spansCount: */ 1);


			internal static readonly SampleAppUrlPathData CustomChildSpanThrowsExceptionPage =
				new SampleAppUrlPathData(HomeController.CustomChildSpanThrowsPageRelativePath, 500, spansCount: 2, errorsCount: 2);


			internal static readonly SampleAppUrlPathData ForbidHttpResponsePageDescriptionPage =
				new SampleAppUrlPathData(HomeController.ForbidHttpResponsePageRelativePath, 200, spansCount: 1, errorsCount: 1);

			internal static readonly SampleAppUrlPathData GetDotNetRuntimeDescriptionPage =
				new SampleAppUrlPathData(HomeController.GetDotNetRuntimeDescriptionPageRelativePath, 200);


			internal static readonly SampleAppUrlPathData ReturnBadRequestPage =
				new SampleAppUrlPathData(HomeController.ReturnBadRequestPageRelativePath, (int)HttpStatusCode.BadRequest);

			/// errorsCount for ThrowsNameCouldNotBeResolvedPage is 0 because we don't automatically capture exceptions
			/// that escaped from Full Framework ASP.NET transactions as errors (yet)
			internal static readonly SampleAppUrlPathData ThrowsInvalidOperationPage =
				new SampleAppUrlPathData(HomeController.ThrowsInvalidOperationPageRelativePath, 500);
		}

		private TimedEvent? _sampleAppClientCallTiming;

		public Task InitializeAsync()
		{
			// Mock APM server should be started only after sample application is started in clean state.
			// The order is important to prevent agent's queued data from the previous test to be sent
			// to this test instance of mock APM server.
			_iisAdministration.SetupSampleAppInCleanState(_envVarsToSetForSampleAppPool, SampleAppShouldHaveAccessToPerfCounters);
			if (_startMockApmServer)
				_mockApmServer.RunInBackground(_mockApmServerPort);
			else
			{
				Logger.Info()
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
				Logger.Warning()
					?.Log("Not tearing down IIS sample application and pool because {Reason}", TearDownPersistentDataReason);
			}

			if (_startMockApmServer) await _mockApmServer.StopAsync();

			Logger.Info()?.Log("Finished test: {FullUnitTestName}", TestDisplayName);
		}

		private string GetSampleAppLogFilePath()
		{
			var sampleAppLogFilePath = Environment.GetEnvironmentVariable(LoggingConfig.LogFileEnvVarName);
			if (sampleAppLogFilePath != null)
			{
				Logger.Info()
					?.Log("Environment variable `{SampleAppLogFileEnvVarName}' is set to `{SampleAppLogFilePath}'"
						+ " - using it to write/read sample application's and agent's log", LoggingConfig.LogFileEnvVarName, sampleAppLogFilePath);
				return sampleAppLogFilePath;
			}

			sampleAppLogFilePath = Path.Combine(Path.GetTempPath(), $"{Consts.SampleApp.AppName}.log");
			Logger.Info()
				?.Log("Environment variable `{SampleAppLogFileEnvVarName}' is not set"
					+ " - using `{SampleAppLogFilePath}' to write/read sample application's and agent's log",
					LoggingConfig.LogFileEnvVarName, sampleAppLogFilePath);
			return sampleAppLogFilePath;
		}

		private static string BuildApmServerUrl(int apmServerPort) => $"http://localhost:{apmServerPort}/";

		protected async Task<SampleAppResponse> SendGetRequestToSampleAppAndVerifyResponse(string relativeUrlPath, int expectedStatusCode,
			bool timeHttpCall = true
		)
		{
			var startTime = DateTime.UtcNow;
			if (timeHttpCall)
			{
				Logger.Debug()
					?.Log("HTTP call to sample application started at {Time} (as timestamp: {Timestamp})",
						startTime, TimeUtils.ToTimestamp(startTime));
			}
			try
			{
				using (var httpClient = new HttpClient())
				{
					var response = await SendGetRequestToSampleAppAndVerifyResponseImpl(httpClient, relativeUrlPath, expectedStatusCode);
					return new SampleAppResponse(response.Headers, await response.Content.ReadAsStringAsync());
				}
			}
			finally
			{
				if (timeHttpCall)
				{
					_sampleAppClientCallTiming.Should().BeNull();
					var endTime = DateTime.UtcNow;
					Logger.Debug()
						?.Log("HTTP call to sample application ended at {Time} (as timestamp: {Timestamp}), Duration: {Duration}ms",
							endTime, TimeUtils.ToTimestamp(endTime),
							TimeUtils.DurationBetweenTimestamps(TimeUtils.ToTimestamp(startTime), TimeUtils.ToTimestamp(endTime)));
					_sampleAppClientCallTiming = new TimedEvent(startTime, endTime);
				}
			}
		}

		private async Task<HttpResponseMessage> SendGetRequestToSampleAppAndVerifyResponseImpl(
			HttpClient httpClient,
			string relativeUrlPath,
			int expectedStatusCode
		)
		{
			var url = Consts.SampleApp.RootUrl + "/" + relativeUrlPath;
			Logger.Debug()?.Log("Sending request with URL: {url} and expected status code: {HttpStatusCode}...", url, expectedStatusCode);
			var response = await httpClient.GetAsync(url);
			Logger.Debug()
				?.Log("Request sent. Actual status code: {HttpStatusCode} ({HttpStatusCodeEnum})",
					(int)response.StatusCode, response.StatusCode);
			try
			{
				response.StatusCode.Should().Be(expectedStatusCode);
			}
			catch (XunitException ex)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				Logger.Error()?.Log("{ExceptionMessage}. Response content:\n{ResponseContent}", ex.Message, responseContent);
				throw;
			}

			var processIdInResponse = response.Headers.GetValues(AspNetFullFrameworkSampleApp.Consts.ProcessIdResponseHeaderName);
			Logger.Debug()
				?.Log("{ProcessIdHeaderName} in response is {ProcessIdHeaderValue}",
					AspNetFullFrameworkSampleApp.Consts.ProcessIdResponseHeaderName, processIdInResponse);

			var apmServerUrlsInResponse =
				response.Headers?.GetValues(AspNetFullFrameworkSampleApp.Consts.ElasticApmServerUrlsResponseHeaderName).ToList();
			try
			{
				apmServerUrlsInResponse.Should().HaveCount(1);
				apmServerUrlsInResponse.First().Should().Be(BuildApmServerUrl(_mockApmServerPort));
			}
			catch (XunitException ex)
			{
				Logger.Error()
					?.LogException(ex, "Sample application's APM-server-URLs configuration setting ({ActualApmServerUrl})" +
						" is different from expected ({ExpectedApmServerUrl})",
						string.Join(", ", apmServerUrlsInResponse), BuildApmServerUrl(_mockApmServerPort));

				await PostTestFailureDiagnostics();

				throw;
			}

			return response;
		}

		protected async Task VerifyDataReceivedFromAgent(Action<ReceivedData> verifyAction)
		{
			var attemptNumber = 0;
			var timerSinceStart = Stopwatch.StartNew();
			while (true)
			{
				++attemptNumber;

				if (!_mockApmServer.ReceivedData.InvalidPayloadErrors.IsEmpty)
				{
					var messageBuilder = new StringBuilder();
					messageBuilder.AppendLine("There is at least one invalid payload error - the test is considered as failed.");
					messageBuilder.AppendLine(TextUtils.Indent("Invalid payload error(s):", 1));
					foreach (var invalidPayloadError in _mockApmServer.ReceivedData.InvalidPayloadErrors)
						messageBuilder.AppendLine(TextUtils.Indent(invalidPayloadError, 2));
					throw new XunitException(messageBuilder.ToString());
				}

				try
				{
					verifyAction(_mockApmServer.ReceivedData);
					timerSinceStart.Stop();
					Logger.Debug()
						?.Log("Data received from agent passed verification." +
							" Time elapsed: {VerificationTimeSeconds}s." +
							" Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}",
							timerSinceStart.Elapsed.TotalSeconds,
							attemptNumber, DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify);
					LogSampleAppLogFileContent();
					await LogSampleAppDiagnosticsPage();
					return;
				}
				catch (XunitException ex)
				{
					var logOnThisAttempt =
						attemptNumber >= DataSentByAgentVerificationConsts.LogMessageAfterNInitialAttempts &&
						attemptNumber % DataSentByAgentVerificationConsts.LogMessageEveryNAttempts == 0;

					if (logOnThisAttempt)
					{
						Logger.Warning()
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
						Logger.Error()?.LogException(ex, "Reached max number of attempts to verify payload - Rethrowing the last exception...");
						await PostTestFailureDiagnostics();
						throw;
					}

					if (logOnThisAttempt)
					{
						Logger.Debug()
							?.Log("Waiting {WaitBetweenVerifyAttemptsMs}ms before the next attempt..." +
								" This message is printed only every {LogMessageEveryNAttempts} attempts",
								DataSentByAgentVerificationConsts.WaitBetweenVerifyAttemptsMs,
								DataSentByAgentVerificationConsts.LogMessageEveryNAttempts);
					}

					Thread.Sleep(DataSentByAgentVerificationConsts.WaitBetweenVerifyAttemptsMs);
				}
				catch (Exception ex)
				{
					Logger.Error()?.LogException(ex, "Exception escaped from verifier");
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
				Logger.Info()?.Log("Sample application log is disabled");
				return;
			}

			string sampleAppLogFileContent;
			try
			{
				sampleAppLogFileContent = File.ReadAllText(_sampleAppLogFilePath);
			}
			catch (Exception ex)
			{
				Logger.Info()
					?.LogException(ex, "Exception thrown while trying to read sample application log file (`{SampleAppLogFilePath}')",
						_sampleAppLogFilePath);
				return;
			}

			Logger.Info()?.Log("Sample application log:\n{SampleAppLogFileContent}", TextUtils.Indent(sampleAppLogFileContent));
		}

		private async Task LogSampleAppDiagnosticsPage()
		{
			var httpClient = new HttpClient();
			const string url = Consts.SampleApp.RootUrl + "/" + DiagnosticsController.DiagnosticsPageRelativePath;
			Logger.Debug()?.Log("Getting content of sample application diagnostics page ({url})...", url);
			var response = await httpClient.GetAsync(url);
			Logger.Debug()
				?.Log("Received sample application's diagnostics page. Status code: {HttpStatusCode} ({HttpStatusCodeEnum})",
					(int)response.StatusCode, response.StatusCode);

			Logger.Info()
				?.Log("Sample application's diagnostics page content:\n{DiagnosticsPageContent}",
					TextUtils.Indent(await response.Content.ReadAsStringAsync()));
		}

		private async Task PostTestFailureDiagnostics()
		{
			_iisAdministration.LogIisApplicationHostConfig();
			LogSampleAppLogFileContent();
			await LogSampleAppDiagnosticsPage();

			Logger.Debug()
				?.Log("Analyzing potential issues... _mockApmServer.ReceivedData: " +
					"#transactions: {NumberOfTransactions}, #spans: {NumberOfSpans}, #errors: {NumberOfErrors}, #metric sets: {NumberOfMetricSets}",
					_mockApmServer.ReceivedData.Transactions.Count,
					_mockApmServer.ReceivedData.Spans.Count,
					_mockApmServer.ReceivedData.Errors.Count,
					_mockApmServer.ReceivedData.Metrics.Count);

			FindReceivedDataWithTimestampEarlierThanTestStart();
		}

		private void FindReceivedDataWithTimestampEarlierThanTestStart()
		{
			foreach (var error in _mockApmServer.ReceivedData.Errors) AnalyzeDtoTimestamp(error.Timestamp, error);
			foreach (var metricSet in _mockApmServer.ReceivedData.Metrics) AnalyzeDtoTimestamp(metricSet.Timestamp, metricSet);
			foreach (var span in _mockApmServer.ReceivedData.Spans) AnalyzeDtoTimestamp(span.Timestamp, span);
			foreach (var transaction in _mockApmServer.ReceivedData.Transactions) AnalyzeDtoTimestamp(transaction.Timestamp, transaction);

			void AnalyzeDtoTimestamp(long dtoTimestamp, object dto)
			{
				var dtoStartTime = TimeUtils.ToDateTime(dtoTimestamp);

				if (_testStartTime <= dtoStartTime) return;

				Logger.Warning()
					?.Log("The following DTO received from the agent has timestamp that is earlier than the current test start time. " +
						"DTO timestamp: {DtoTimestamp}, test start time: {TestStartTime}, DTO: {DtoFromAgent}",
						dtoStartTime.ToLocalTime().FormatForLog(), _testStartTime.ToLocalTime().FormatForLog(), dto);
			}
		}

		protected async Task VerifyDataReceivedFromAgent(SampleAppUrlPathData sampleAppUrlPathData) =>
			await VerifyDataReceivedFromAgent(receivedData => { TryVerifyDataReceivedFromAgent(sampleAppUrlPathData, receivedData); });

		protected void TryVerifyDataReceivedFromAgent(SampleAppUrlPathData sampleAppUrlPathData, ReceivedData receivedData)
		{
			FullFwAssertValid(receivedData);

			receivedData.Transactions.Count.Should().Be(sampleAppUrlPathData.TransactionsCount);
			receivedData.Spans.Count.Should().Be(sampleAppUrlPathData.SpansCount);
			receivedData.Errors.Count.Should().Be(sampleAppUrlPathData.ErrorsCount);

			// ReSharper disable once InvertIf
			if (receivedData.Transactions.Count == 1)
			{
				var transaction = receivedData.Transactions.First();

				if (transaction.Context != null)
				{
					transaction.Context.Request.Url.Full.Should().Be(Consts.SampleApp.RootUrl + "/" + sampleAppUrlPathData.RelativeUrlPath);

					var questionMarkIndex = sampleAppUrlPathData.RelativeUrlPath.IndexOf('?');
					if (questionMarkIndex == -1)
					{
						transaction.Context.Request.Url.PathName.Should()
							.Be(Consts.SampleApp.RootUrlPath + "/" + sampleAppUrlPathData.RelativeUrlPath);
						transaction.Context.Request.Url.Search.Should().BeNull();
					}
					else
					{
						transaction.Context.Request.Url.PathName.Should()
							.Be(Consts.SampleApp.RootUrlPath + "/" + sampleAppUrlPathData.RelativeUrlPath.Substring(0, questionMarkIndex));
						transaction.Context.Request.Url.Search.Should().Be(sampleAppUrlPathData.RelativeUrlPath.Substring(questionMarkIndex + 1));
					}

					transaction.Context.Response.StatusCode.Should().Be(sampleAppUrlPathData.StatusCode);
				}

				var httpStatusFirstDigit = sampleAppUrlPathData.StatusCode / 100;
				transaction.Result.Should().Be($"HTTP {httpStatusFirstDigit}xx");
				transaction.SpanCount.Started.Should().Be(sampleAppUrlPathData.SpansCount);
			}
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
			foreach (var error in receivedData.Errors) FullFwAssertValid(error);
			foreach (var metadata in receivedData.Metadata) FullFwAssertValid(metadata);
			foreach (var metricSet in receivedData.Metrics) FullFwAssertValid(metricSet);
			foreach (var span in receivedData.Spans) FullFwAssertValid(span);
			foreach (var transaction in receivedData.Transactions) FullFwAssertValid(transaction);
		}

		private void FullFwAssertValid(MetadataDto metadata)
		{
			metadata.Should().NotBeNull();

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

			service.Name.Should().Be(expectedServiceName);
			service.Environment.Should().Be(expectedEnvironment);
		}

		private static void FullFwAssertValid(Framework framework)
		{
			framework.Should().NotBeNull();

			framework.Name.Should().Be("ASP.NET");
			framework.Version.Should().StartWith("4.");
		}

		private static void FullFwAssertValid(Api.System system) => system.Should().BeNull();

		private void FullFwAssertValid(ErrorDto error)
		{
			FullFwAssertValid((ITimestampedDto)error);
			error.Transaction.AssertValid();
			if (error.Context != null) FullFwAssertValid(error.Context, error);
			error.Culprit.NonEmptyAssertValid();
			error.Exception.AssertValid();
		}

		private void FullFwAssertValid(TransactionDto transaction)
		{
			transaction.Should().NotBeNull();

			if (transaction.Context != null) FullFwAssertValid(transaction.Context, transaction);
			transaction.Name.Should().NotBeNull();
			TransactionResultFullFwAssertValid(transaction.Result);
			transaction.Type.Should().Be(ApiConstants.TypeRequest);
			FullFwAssertValid(transaction.SpanCount);
			FullFwAssertValid((ITimedDto)transaction);
		}

		private void FullFwAssertValid(ITimestampedDto timestampedDto)
		{
			timestampedDto.Should().NotBeNull();

			if (_sampleAppClientCallTiming != null) timestampedDto.ShouldOccurBetween(_sampleAppClientCallTiming);
		}

		private void FullFwAssertValid(ITimedDto timedDto)
		{
			FullFwAssertValid((ITimestampedDto)timedDto);

			if (_sampleAppClientCallTiming != null) timedDto.ShouldOccurBetween(_sampleAppClientCallTiming);
		}

		private static void FullFwAssertValid(SpanCountDto spanCount)
		{
			spanCount.Should().NotBeNull();

			spanCount.Dropped.Should().Be(0);
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
			FullFwAssertValid(span.StackTrace);
		}

		private static void FullFwAssertValid(IEnumerable<CapturedStackFrame> stackTrace)
		{
			// ReSharper disable PossibleMultipleEnumeration
			stackTrace.Should().NotBeNull();

			stackTrace.ForEach((Action<CapturedStackFrame>)FullFwAssertValid);
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

			socket.Encrypted.Should().BeFalse();
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
			internal string ServiceName;
		}

		public class SampleAppUrlPathData
		{
			public readonly int ErrorsCount;
			public readonly string RelativeUrlPath;
			public readonly int SpansCount;
			public readonly int StatusCode;
			public readonly int TransactionsCount;

			public SampleAppUrlPathData(string relativeUrlPath, int statusCode, int transactionsCount = 1, int spansCount = 0, int errorsCount = 0)
			{
				RelativeUrlPath = relativeUrlPath;
				StatusCode = statusCode;
				TransactionsCount = transactionsCount;
				SpansCount = spansCount;
				ErrorsCount = errorsCount;
			}

			public SampleAppUrlPathData Clone(
				string relativeUrlPath = null,
				int? status = null,
				int? transactionsCount = null,
				int? spansCount = null,
				int? errorsCount = null
			) => new SampleAppUrlPathData(
				relativeUrlPath ?? RelativeUrlPath,
				status ?? StatusCode,
				transactionsCount ?? TransactionsCount,
				spansCount ?? SpansCount,
				errorsCount ?? ErrorsCount);
		}
	}
}
