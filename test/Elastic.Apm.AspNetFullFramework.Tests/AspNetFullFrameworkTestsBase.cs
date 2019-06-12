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
	public class AspNetFullFrameworkTestsBase : IAsyncLifetime
	{
		private const int MaxNumberOfAttemptsToVerify = 10;
		private const int WaitBetweenVerifyAttemptsMs = 1000;

		private readonly IApmLogger _logger;
		private readonly MockApmServer _mockApmServer;
		private readonly bool _startMockApmServer;
		private readonly DateTimeOffset _testStartTime = DateTimeOffset.UtcNow + TimeSpan.FromHours(1);

		protected AspNetFullFrameworkTestsBase(ITestOutputHelper xUnitOutputHelper, bool startMockApmServer = true)
		{
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(AspNetFullFrameworkTestsBase));
			_mockApmServer = new MockApmServer(_logger, GetCurrentTestName(xUnitOutputHelper));
			_startMockApmServer = startMockApmServer;
		}

		public Task InitializeAsync()
		{
			if (_startMockApmServer) _mockApmServer.RunAsync();

			IisAdministration.EnsureSampleAppIsRunningInCleanState(_logger);

			return Task.CompletedTask;
		}

		public async Task DisposeAsync()
		{
			IisAdministration.RemoveSampleAppFromIis(_logger);

			if (_startMockApmServer) await _mockApmServer.StopAsync();
		}

		protected async Task<HttpResponseMessage> SendGetRequestToSampleApp(string urlPath)
		{
			var httpClient = new HttpClient();
			return await httpClient.GetAsync(Consts.SampleApp.RootUri + "/" + urlPath);
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
							attemptNumber, MaxNumberOfAttemptsToVerify);
					return;
				}
				catch (XunitException ex)
				{
					_logger.Debug()
						?.LogException(ex, "Payload verification failed. Attempt #{AttemptNumber} out of {MaxNumberOfAttempts}",
							attemptNumber, MaxNumberOfAttemptsToVerify);

					if (attemptNumber == MaxNumberOfAttemptsToVerify)
					{
						_logger.Error()?.LogException(ex, "Reached max number of attempts to verify payload - Rethrowing the last exception...");
						AnalyzePotentialIssues();
						throw;
					}

					_logger.Debug()?.Log("Waiting {WaitTimeMs}ms before the next attempt...", WaitBetweenVerifyAttemptsMs);
					Thread.Sleep(WaitBetweenVerifyAttemptsMs);
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
