using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Logging;
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
		private static readonly bool KeepIisItems = EnvVarUtils.GetBoolValue("ELASTIC_APM_TESTS_FULL_FRAMEWORK_KEEP_IIS_ITEMS", /* defaultValue: */ false);

		private static class DataSentByAgentVerificationConsts
		{
			internal const int MaxNumberOfAttemptsToVerify = 10;
			internal const int WaitBetweenVerifyAttemptsMs = 1000;
		}

		private readonly IApmLogger _logger;
		private readonly MockApmServer _mockApmServer;
		private readonly IisAdministration _iisAdministration;
		private readonly bool _startMockApmServer;
		private readonly DateTimeOffset _testStartTime = DateTimeOffset.UtcNow;

		protected TestsBase(ITestOutputHelper xUnitOutputHelper, bool startMockApmServer = true)
		{
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(TestsBase));
			_mockApmServer = new MockApmServer(_logger, GetCurrentTestName(xUnitOutputHelper));
			_iisAdministration = new IisAdministration(_logger);
			_startMockApmServer = startMockApmServer;
		}

		public Task InitializeAsync()
		{
			int mockApmServerPort = _mockApmServer.FindAvailablePortToListen();
			// Mock APM server should be started only after sample application is started in clean state.
			// The order is important to prevent agent's queued data from the previous test to be sent
			// to this test instance of mock APM server.
			_iisAdministration.SetupSampleAppInCleanState(mockApmServerPort);
			if (_startMockApmServer) _mockApmServer.RunAsync(mockApmServerPort);

			return Task.CompletedTask;
		}

		public async Task DisposeAsync()
		{
			if (!KeepIisItems) _iisAdministration.DisposeSampleApp();

			if (_startMockApmServer) await _mockApmServer.StopAsync();
		}

		protected async Task SendGetRequestToSampleAppAndVerifyResponseStatusCode(string urlPath, int expectedStatusCode)
		{
			var httpClient = new HttpClient();
			var response = await httpClient.GetAsync(Consts.SampleApp.RootUri + "/" + urlPath);
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

		protected void VerifyPayloadFromAgent(Action<ReceivedData> verifier)
		{
			_mockApmServer.ReceivedData.InvalidPayloadErrors.Should().BeEmpty();

			var attemptNumber = 0;
			while (true)
			{
				++attemptNumber;
				try
				{
					verifier(_mockApmServer.ReceivedData);
					_logger.Debug()
						?.Log("Payload verification succeeded. Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}",
							attemptNumber, DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify);
					return;
				}
				catch (XunitException ex)
				{
					_logger.Debug()
						?.LogException(ex, "Payload verification failed. Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}",
							attemptNumber, DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify);

					if (attemptNumber == DataSentByAgentVerificationConsts.MaxNumberOfAttemptsToVerify)
					{
						_logger.Error()?.LogException(ex, "Reached max number of attempts to verify payload - Rethrowing the last exception...");
						AnalyzePotentialIssues();
						throw;
					}

					_logger.Debug()?.Log("Waiting {WaitTimeMs}ms before the next attempt...", DataSentByAgentVerificationConsts.WaitBetweenVerifyAttemptsMs);
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
		public static IEnumerable<object[]> GenerateSampleAppUrlPathsData()
		{
			yield return new object[] { new SampleAppUrlPathData("", 200) };
			yield return new object[] { new SampleAppUrlPathData(Consts.SampleApp.HomePageRelativePath, 200) };

			// Contact page processing does HTTP Get for About page (additional transaction) and https://elastic.co/ - so 2 spans
			yield return new object[] { new SampleAppUrlPathData(Consts.SampleApp.ContactPageRelativePath, 200, 2, 2) };

			yield return new object[] { new SampleAppUrlPathData("Dummy_nonexistent_path", 404) };
		}

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
				var dtoStartTime = DateTimeOffset.FromUnixTimeMilliseconds(dtoTimestamp / 1000);

				if (_testStartTime <= dtoStartTime) return;

				_logger.Warning()
					?.Log("The following DTO received from the agent has timestamp that is earlier than the current test start time. " +
						"DTO timestamp: {DtoTimestamp}, test start time: {TestStartTime}, DTO: {DtoFromAgent}",
						dtoStartTime.LocalDateTime, _testStartTime.LocalDateTime, dto);
			}
		}

		public class SampleAppUrlPathData
		{
			public readonly int SpansCount;
			public readonly int Status;
			public readonly int TransactionsCount;
			public readonly string UrlPath;

			public SampleAppUrlPathData(string urlPath, int status, int transactionsCount = 1, int spansCount = 0)
			{
				UrlPath = urlPath;
				Status = status;
				TransactionsCount = transactionsCount;
				SpansCount = spansCount;
			}
		}
	}
}
