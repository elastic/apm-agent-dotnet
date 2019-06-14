using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Helpers;
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
		private static readonly string TearDownPersistentDataReason;

		private static readonly bool TearDownPersistentData =
			EnvVarUtils.GetBoolValue("ELASTIC_APM_TESTS_FULL_FRAMEWORK_TEAR_DOWN_PERSISTENT_DATA", /* defaultValue: */ true,
				out TearDownPersistentDataReason);

		private readonly Dictionary<string, string> _envVarsToSetForSampleAppPool;
		private readonly IisAdministration _iisAdministration;

		private readonly IApmLogger _logger;
		private readonly MockApmServer _mockApmServer;
		private readonly int _mockApmServerPort;
		private readonly bool _startMockApmServer;
		protected readonly bool SampleAppShouldHaveAccessToPerfCounters;
		private readonly DateTimeOffset _testStartTime = DateTimeOffset.UtcNow;

		protected TestsBase(ITestOutputHelper xUnitOutputHelper,
			bool startMockApmServer = true,
			Dictionary<string, string> envVarsToSetForSampleAppPool = null,
			bool sampleAppShouldHaveAccessToPerfCounters = false)
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
			internal static readonly SampleAppUrlPathData PageCallingAnotherPage =
				new SampleAppUrlPathData(Consts.SampleApp.ContactPageRelativePath, 200, 2, 2);

			internal static readonly List<SampleAppUrlPathData> AllPaths = new List<SampleAppUrlPathData>()
			{
				new SampleAppUrlPathData("", 200),
				new SampleAppUrlPathData(Consts.SampleApp.HomePageRelativePath, 200),
				PageCallingAnotherPage,
				new SampleAppUrlPathData("Dummy_nonexistent_path", 404),
			};
		}

		protected ExpectedNonDefaultsS ExpectedNonDefaults = new ExpectedNonDefaultsS();

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
				_logger.Warning()
					?.Log("Not tearing down IIS sample application and pool because {Reason}", TearDownPersistentData);

			if (_startMockApmServer) await _mockApmServer.StopAsync();
		}

		protected async Task SendGetRequestToSampleAppAndVerifyResponseStatusCode(string urlPath, int expectedStatusCode)
		{
			var httpClient = new HttpClient();
			var url = Consts.SampleApp.RootUrl + "/" + urlPath;
			_logger.Debug()?.Log("Sending request with URL: {url} and expected status code: {HttpStatusCode}...", url, expectedStatusCode);
			var response = await httpClient.GetAsync(url);
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

		protected void VerifyPayloadFromAgent(Action<ReceivedData> verifyAction)
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
				var dtoStartTime = DateTimeOffset.FromUnixTimeMilliseconds(dtoTimestamp / 1000);

				if (_testStartTime <= dtoStartTime) return;

				_logger.Warning()
					?.Log("The following DTO received from the agent has timestamp that is earlier than the current test start time. " +
						"DTO timestamp: {DtoTimestamp}, test start time: {TestStartTime}, DTO: {DtoFromAgent}",
						dtoStartTime.LocalDateTime, _testStartTime.LocalDateTime, dto);
			}
		}

		protected void VerifyDataReceivedFromAgent(SampleAppUrlPathData sampleAppUrlPathData) =>
			VerifyPayloadFromAgent(receivedData =>
			{
				receivedData.Transactions.Count.Should().Be(sampleAppUrlPathData.TransactionsCount);
				receivedData.Spans.Count.Should().Be(sampleAppUrlPathData.SpansCount);

				VerifyMetadata(receivedData);
			});

		private void VerifyMetadata(ReceivedData receivedData)
		{
			foreach (var metadata in receivedData.Metadata)
			{
				metadata.Service.Agent.Name.Should().Be(Apm.Consts.AgentName);
				metadata.Service.Agent.Version.Should().Be(Assembly.Load("Elastic.Apm").GetName().Version.ToString());
				metadata.Service.Framework.Name.Should().Be("ASP.NET");
				metadata.Service.Framework.Version.Should().StartWith("4.");
				metadata.Service.Language.Name.Should().Be("C#");

				string expectedServiceName;
				if (ExpectedNonDefaults.ServiceName == null)
					expectedServiceName = AbstractConfigurationReader.AdaptServiceName($"{Consts.SampleApp.SiteName}_{Consts.SampleApp.AppPoolName}");
				else
					expectedServiceName = ExpectedNonDefaults.ServiceName;
				metadata.Service.Name.Should().Be(expectedServiceName);

				// Capturing information about Docker container is not implemented for Windows yet
//				Assert.True(metadata.System.Container.Id == null);
				metadata.System?.Container.Should().BeNull();
			}
		}

		protected struct ExpectedNonDefaultsS
		{
			internal string ServiceName;
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
