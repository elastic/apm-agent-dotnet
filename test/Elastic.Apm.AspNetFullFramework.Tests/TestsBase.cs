using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AspNetFullFrameworkSampleApp.Controllers;
using Elastic.Apm.Api;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
using Elastic.Apm.Logging;
using Elastic.Apm.Model;
using Elastic.Apm.Tests.MockApmServer;
using Elastic.Apm.Tests.TestHelpers;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	public class TestsBase : IAsyncLifetime
	{
		private static readonly string TearDownPersistentDataReason;

		private static readonly bool TearDownPersistentData =
			EnvVarUtils.GetBoolValue("ELASTIC_APM_TESTS_FULL_FRAMEWORK_TEAR_DOWN_PERSISTENT_DATA", /* defaultValue: */ true,
				out TearDownPersistentDataReason);


		protected readonly bool SampleAppShouldHaveAccessToPerfCounters;

		private readonly Dictionary<string, string> _envVarsToSetForSampleAppPool;
		private readonly IisAdministration _iisAdministration;

		private readonly IApmLogger _logger;
		private readonly MockApmServer _mockApmServer;
		private readonly int _mockApmServerPort;
		private readonly bool _startMockApmServer;
		private readonly DateTimeOffset _testStartTime = DateTimeOffset.UtcNow;

		protected TestsBase(ITestOutputHelper xUnitOutputHelper,
			bool startMockApmServer = true,
			Dictionary<string, string> envVarsToSetForSampleAppPool = null,
			bool sampleAppShouldHaveAccessToPerfCounters = false
		)
		{
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(TestsBase));
			_mockApmServer = new MockApmServer(_logger, GetCurrentTestName(xUnitOutputHelper));
			_iisAdministration = new IisAdministration(_logger);
			_startMockApmServer = startMockApmServer;
			SampleAppShouldHaveAccessToPerfCounters = sampleAppShouldHaveAccessToPerfCounters;

			_mockApmServerPort = _startMockApmServer ? _mockApmServer.FindAvailablePortToListen() : ConfigConsts.DefaultValues.ApmServerPort;

			_envVarsToSetForSampleAppPool = envVarsToSetForSampleAppPool == null
				? new Dictionary<string, string>()
				: new Dictionary<string, string>(envVarsToSetForSampleAppPool);
			_envVarsToSetForSampleAppPool.TryAdd(ConfigConsts.EnvVarNames.ServerUrls, $"http://localhost:{_mockApmServerPort}");
		}

		private static class DataSentByAgentVerificationConsts
		{
			internal const int MaxNumberOfAttemptsToVerify = 100;
			internal const int WaitBetweenVerifyAttemptsMs = 100;
		}

		internal static class SampleAppUrlPaths
		{
			/// Contact page processing does HTTP Get for About page (additional transaction) and https://elastic.co/ - so 2 spans
			internal static readonly SampleAppUrlPathData ContactPage =
				new SampleAppUrlPathData(HomeController.ContactPageRelativePath, 200, 2, 2);

			internal static readonly SampleAppUrlPathData CustomSpanThrowsExceptionPage =
				new SampleAppUrlPathData(HomeController.CustomSpanThrowsPageRelativePath, 500, spansCount: 1);

			internal static readonly SampleAppUrlPathData HomePage = new SampleAppUrlPathData(HomeController.HomePageRelativePath, 200);

			internal static readonly List<SampleAppUrlPathData> AllPaths = new List<SampleAppUrlPathData>()
			{
				new SampleAppUrlPathData("", 200),
				HomePage,
				ContactPage,
				CustomSpanThrowsExceptionPage,
				new SampleAppUrlPathData("Dummy_nonexistent_path", 404),
			};
		}

		protected AgentConfiguration AgentConfig = new AgentConfiguration();

		public Task InitializeAsync()
		{
			// Mock APM server should be started only after sample application is started in clean state.
			// The order is important to prevent agent's queued data from the previous test to be sent
			// to this test instance of mock APM server.
			_iisAdministration.SetupSampleAppInCleanState(_envVarsToSetForSampleAppPool, SampleAppShouldHaveAccessToPerfCounters);
			if (_startMockApmServer)
				_mockApmServer.RunAsync(_mockApmServerPort);
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

			if (_startMockApmServer) await _mockApmServer.StopAsync();
		}

		protected async Task SendGetRequestToSampleAppAndVerifyResponseStatusCode(string relativeUrlPath, int expectedStatusCode)
		{
			var httpClient = new HttpClient();
			var url = Consts.SampleApp.RootUrl + "/" + relativeUrlPath;
			_logger.Debug()?.Log("Sending request with URL: {url} and expected status code: {HttpStatusCode}...", url, expectedStatusCode);
			var response = await httpClient.GetAsync(url);
			_logger.Debug()
				?.Log("Request sent. Actual status code: {HttpStatusCode} ({HttpStatusCodeEnum})",
					(int)response.StatusCode, response.StatusCode);
			try
			{
				response.StatusCode.Should().Be(expectedStatusCode);
			}
			catch (XunitException ex)
			{
				var responseContent = await response.Content.ReadAsStringAsync();
				_logger.Error()?.Log("{ExceptionMessage}. Response content:\n{ResponseContent}", ex.Message, responseContent);
				throw;
			}
		}

		protected void VerifyDataReceivedFromAgent(Action<ReceivedData> verifyAction)
		{
			_mockApmServer.ReceivedData.InvalidPayloadErrors.Should().BeEmpty();

			var attemptNumber = 0;
			while (true)
			{
				++attemptNumber;
				try
				{
					verifyAction(_mockApmServer.ReceivedData);
					_logger.Debug()
						?.Log("Data received from agent passed verification. Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}",
							attemptNumber, DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify);
					return;
				}
				catch (XunitException ex)
				{
					_logger.Debug()
						?.LogException(ex,
							"Data received from agent did NOT pass verification. Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}",
							attemptNumber, DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify);

					if (attemptNumber == DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify)
					{
						_logger.Error()?.LogException(ex, "Reached max number of attempts to verify payload - Rethrowing the last exception...");
						AnalyzePotentialIssues();
						throw;
					}

					_logger.Debug()
						?.Log("Waiting {WaitTimeMs}ms before the next attempt...", DataSentByAgentVerificationConsts.WaitBetweenVerifyAttemptsMs);
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
		public static IEnumerable<object[]> AllSampleAppUrlPaths()
		{
			foreach (var data in SampleAppUrlPaths.AllPaths) yield return new object[] { data };
		}

		public static SampleAppUrlPathData RandomSampleAppUrlPath() =>
			SampleAppUrlPaths.AllPaths[RandomGenerator.GetInstance().Next(0, SampleAppUrlPaths.AllPaths.Count)];

		private static string GetCurrentTestName(ITestOutputHelper xUnitOutputHelper)
		{
			var helper = (TestOutputHelper)xUnitOutputHelper;

			var test = (ITest)helper.GetType()
				.GetField("test", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(helper);

			return test.TestCase.TestMethod.Method.Name;
		}

		private void AnalyzePotentialIssues()
		{
			_logger.Debug()
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
				var dtoStartTime = TimeUtils.TimestampToDateTimeOffset(dtoTimestamp);

				if (_testStartTime <= dtoStartTime) return;

				_logger.Warning()
					?.Log("The following DTO received from the agent has timestamp that is earlier than the current test start time. " +
						"DTO timestamp: {DtoTimestamp}, test start time: {TestStartTime}, DTO: {DtoFromAgent}",
						dtoStartTime.LocalDateTime, _testStartTime.LocalDateTime, dto);
			}
		}

		protected void VerifyDataReceivedFromAgent(SampleAppUrlPathData sampleAppUrlPathData) =>
			VerifyDataReceivedFromAgent(receivedData => { TryVerifyDataReceivedFromAgent(sampleAppUrlPathData, receivedData); });

		protected void TryVerifyDataReceivedFromAgent(SampleAppUrlPathData sampleAppUrlPathData, ReceivedData receivedData)
		{
			FullFwAssertValid(receivedData);

			receivedData.Transactions.Count.Should().Be(sampleAppUrlPathData.TransactionsCount);
			receivedData.Spans.Count.Should().Be(sampleAppUrlPathData.SpansCount);

			if (receivedData.Transactions.Count == 1)
			{
				var transaction = receivedData.Transactions.First();

				transaction.Context.Request.Url.Full.Should().Be(Consts.SampleApp.RootUrl + "/" + sampleAppUrlPathData.RelativeUrlPath);

				var questionMarkIndex = sampleAppUrlPathData.RelativeUrlPath.IndexOf('?');
				string pathPart;
				string queryStringPart;
				if (questionMarkIndex == -1)
				{
					pathPart = sampleAppUrlPathData.RelativeUrlPath;
					queryStringPart = null;
				}
				else
				{
					pathPart = sampleAppUrlPathData.RelativeUrlPath.Substring(0, questionMarkIndex);
					queryStringPart = sampleAppUrlPathData.RelativeUrlPath.Substring(questionMarkIndex + 1);
				}

				transaction.Context.Request.Url.PathName.Should().Be(Consts.SampleApp.RootUrlPath + "/" + pathPart);
				if (queryStringPart == null)
					transaction.Context.Request.Url.Search.Should().BeNull();
				else
					transaction.Context.Request.Url.Search.Should().Be(queryStringPart);

				var httpStatusFirstDigit = sampleAppUrlPathData.Status / 100;
				transaction.Result.Should().Be($"HTTP {httpStatusFirstDigit}xx");
				transaction.Context.Response.StatusCode.Should().Be(sampleAppUrlPathData.Status);
			}
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

			string expectedServiceName;
			if (AgentConfig.ServiceName == null)
				expectedServiceName = AbstractConfigurationReader.AdaptServiceName($"{Consts.SampleApp.SiteName}_{Consts.SampleApp.AppPoolName}");
			else
				expectedServiceName = AgentConfig.ServiceName;
			service.Name.Should().Be(expectedServiceName);
		}

		private void FullFwAssertValid(Framework framework)
		{
			framework.Should().NotBeNull();

			framework.Name.Should().Be("ASP.NET");
			framework.Version.Should().StartWith("4.");
		}

		private void FullFwAssertValid(Api.System system) => system.Should().BeNull();

		private void FullFwAssertValid(ErrorDto error)
		{
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
		}

		private void FullFwAssertValid(Url url)
		{
			url.Should().NotBeNull();

			url.Full.Should().NotBeNull();
			url.Raw.Should().Be(url.Full);
			url.Protocol.Should().Be("HTTP");
			url.HostName.Should().Be(Consts.SampleApp.Host);
			url.PathName.Should().NotBeNull();
		}

		private void TransactionResultFullFwAssertValid(string result) => result.Should().MatchRegex("HTTP [1-9]xx");

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

			FullFwAssertValid(span.StackTrace);
		}

		private void FullFwAssertValid(List<CapturedStackFrame> stackTrace)
		{
			stackTrace.Should().NotBeNull();

			foreach (var stackFrame in stackTrace) FullFwAssertValid(stackFrame);
		}

		private void FullFwAssertValid(CapturedStackFrame capturedStackFrame)
		{
			capturedStackFrame.Should().NotBeNull();

			capturedStackFrame.Function.NonEmptyAssertValid();
		}

		private void FullFwAssertValid(MetricSetDto metricSet)
		{
			metricSet.Should().NotBeNull();

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

			var caseInsensitiveRequestHeaders = new Dictionary<string, string>(request.Headers, StringComparer.OrdinalIgnoreCase);
			caseInsensitiveRequestHeaders["Host"].Should().Be(Consts.SampleApp.Host);
		}

		private void FullFwAssertValid(Socket socket)
		{
			socket.Should().NotBeNull();

			socket.Encrypted.Should().BeFalse();
			socket.RemoteAddress.Should().BeOneOf("::1", "127.0.0.1");
		}

		private void FullFwAssertValid(Response response)
		{
			response.Should().NotBeNull();

			response.Headers.Should().NotBeNull();
			response.Finished.Should().BeTrue();
		}

		protected struct AgentConfiguration
		{
			internal string ServiceName;
		}

		public class SampleAppUrlPathData
		{
			public readonly string RelativeUrlPath;
			public readonly int SpansCount;
			public readonly int Status;
			public readonly int TransactionsCount;

			public SampleAppUrlPathData(string relativeUrlPath, int status, int transactionsCount = 1, int spansCount = 0)
			{
				RelativeUrlPath = relativeUrlPath;
				Status = status;
				TransactionsCount = transactionsCount;
				SpansCount = spansCount;
			}
		}
	}
}
