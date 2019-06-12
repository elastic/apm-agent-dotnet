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
		private const int MaxNumberOfAttemptsToVerify = 10;
		private const int WaitBetweenVerifyAttemptsMs = 1000;

		private readonly IApmLogger _logger;
		private readonly MockApmServer _mockApmServer;
		private readonly bool _startMockApmServer;

		protected TestsBase(ITestOutputHelper xUnitOutputHelper, bool startMockApmServer = true)
		{
			_logger = new XunitOutputLogger(xUnitOutputHelper).Scoped(nameof(TestsBase));
			_mockApmServer = new MockApmServer(_logger, GetCurrentTestName(xUnitOutputHelper));
			_startMockApmServer = startMockApmServer;
		}

		public Task InitializeAsync()
		{
			if (_startMockApmServer) _mockApmServer.RunAsync();

			IisAdministration.EnsureSampleAppIsRunning(_logger);

			return Task.CompletedTask;
		}

		public async Task DisposeAsync()
		{
			if (_startMockApmServer) await _mockApmServer.StopAsync();
		}

		protected async Task<HttpResponseMessage> SendGetRequestToSampleApp(string urlPath)
		{
			var httpClient = new HttpClient();
			return await httpClient.GetAsync(Consts.SampleApp.rootUri + "/" + urlPath);
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
			yield return new object[] { new SampleAppUrlPathData(Consts.SampleApp.homePageRelativePath, 200) };

			// Contact page processing does HTTP Get for About page (additional transaction) and https://elastic.co/ - so 2 spans
			yield return new object[] { new SampleAppUrlPathData(Consts.SampleApp.contactPageRelativePath, 200, 2, 2) };

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

		public class SampleAppUrlPathData
		{
			public readonly int Status;
			public readonly int TransactionsCount;
			public readonly string UrlPath;
			public readonly int SpansCount;

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
