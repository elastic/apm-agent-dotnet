using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Elastic.Apm.Config;
using Elastic.Apm.Logging;
using Elastic.Apm.Tests.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Elastic.Apm.AspNetFullFramework.Tests
{
	[Collection(Consts.AspNetFullFrameworkTestsCollection)]
	public class CentralConfigTests : TestsBase
	{
		private const string CustomEnvironment = "AspNetFullFramework_Tests_CentralConfigTests-CustomEnvironment";
		private const string CustomServiceName = "AspNetFullFramework_Tests_CentralConfigTests-CustomServiceName";

		private const string TransactionSampleRateKey = "transaction_sample_rate";

		private readonly ConfigState _configState;

		public CentralConfigTests(ITestOutputHelper xUnitOutputHelper)
			: base(xUnitOutputHelper,
				envVarsToSetForSampleAppPool: new Dictionary<string, string>
				{
					{ ConfigConsts.EnvVarNames.ServiceName, CustomServiceName },
					{ ConfigConsts.EnvVarNames.Environment, CustomEnvironment }
				})
		{
			_configState = new ConfigState(LoggerBase);

			AgentConfig.ServiceName = AbstractConfigurationReader.AdaptServiceName(CustomServiceName);
			AgentConfig.Environment = CustomEnvironment;
		}

		[AspNetFullFrameworkFact]
		public async Task TransactionSample_test()
		{
			var srcPageData = SampleAppUrlPaths.CustomChildSpanThrowsExceptionPage;
			// AssertReceivedDataSampledStatus(receivedData, isSampled, pageData.SpansCount); below relies on number of transactions being 1
			srcPageData.TransactionsCount.Should().Be(1);

			MockApmServer.GetAgentsConfig = (httpRequest, httpResponse) => _configState.BuildResponse(httpRequest, httpResponse);

			// Send first request before updating central configuration to make sure application is running
			await SendGetRequestToSampleAppAndVerifyResponse(srcPageData.RelativeUrlPath, srcPageData.StatusCode);
			await WaitAndVerifyReceivedDataSharedConstraints(srcPageData, /* shouldGatherDiagnostics: */ false);

			var isSampledPerStep = new bool?[] { false, false, true, null, null, true, true, false, null };

			await isSampledPerStep.ForEachIndexed(async (isSampledNullable, index) =>
			{
				ClearState();

				await _configState.UpdateAndWaitForAgentToApply(new Dictionary<string, string>
				{
					{ TransactionSampleRateKey, isSampledNullable.HasValue ? isSampledNullable.Value ? "1" : "0" : null }
				});

				var isSampled = !isSampledNullable.HasValue || isSampledNullable.Value;
				var pageData = isSampled ? srcPageData : srcPageData.Clone(spansCount: 0);
				await SendGetRequestToSampleAppAndVerifyResponse(pageData.RelativeUrlPath, pageData.StatusCode);

				var isLastStep = index == isSampledPerStep.Length - 1;
				await WaitAndCustomVerifyReceivedData(receivedData =>
				{
					VerifyReceivedDataSharedConstraints(pageData, receivedData);

					// Relies on srcPageData.TransactionsCount.Should().Be(1);
					AssertReceivedDataSampledStatus(receivedData, isSampled, pageData.SpansCount);
				}, /* shouldGatherDiagnostics: */ isLastStep);
			});
		}

		private class ConfigState
		{
			private const string ThisClassName = nameof(CentralConfigTests) + "." + nameof(ConfigState);

			private readonly object _lock = new object();

			private readonly IApmLogger _logger;

			private readonly Dictionary<string, string> _options = new Dictionary<string, string>();

			internal ConfigState(IApmLogger logger)
			{
				_logger = logger.Scoped(ThisClassName);
				_appliedByAgent = new TaskCompletionSource<object>();
			}

			private TaskCompletionSource<object> _appliedByAgent;

			private int _eTagAsInt = -1;
			private TimeSpan _waitUntilNextRequest = TimeSpan.FromSeconds(1);

			private (string, TaskCompletionSource<object>) Update(
				IReadOnlyDictionary<string, string> optionsToUpdate
				, TimeSpan? waitUntilNextRequest = null
			)
			{
				lock (_lock)
				{
					if (waitUntilNextRequest.HasValue) _waitUntilNextRequest = waitUntilNextRequest.Value;
					// ReSharper disable once UseDeconstructionOnParameter
					optionsToUpdate.ForEach(optToUpdate =>
					{
						if (optToUpdate.Value == null)
							_options.Remove(optToUpdate.Key);
						else
							_options[optToUpdate.Key] = optToUpdate.Value;
					});
					++_eTagAsInt;
					_appliedByAgent.TrySetCanceled();
					_appliedByAgent = new TaskCompletionSource<object>();
					return ($"[ETag: {EtagToString(_eTagAsInt)}] {JsonConvert.SerializeObject(_options)}", _appliedByAgent);
				}
			}

			internal async Task UpdateAndWaitForAgentToApply(
				IReadOnlyDictionary<string, string> optionsToUpdate
				, TimeSpan? waitUntilNextRequest = null
			)
			{
				var (dbgConfig, agentAppliedEvent) = Update(optionsToUpdate, waitUntilNextRequest);
				_logger.Debug()
					?.Log("Waiting for agent to apply updated central configuration..."
						+ " Central config options: {CentralConfigOptions}", dbgConfig);
				await agentAppliedEvent.Task;
			}

			internal IActionResult BuildResponse(HttpRequest httpRequest, HttpResponse httpResponse)
			{
				lock (_lock)
				{
					try
					{
						return BuildResponseImpl(httpRequest, httpResponse);
					}
					catch (Exception ex)
					{
						_logger.Error()?.LogException(ex, nameof(BuildResponseImpl) + " thrown exception");
						_appliedByAgent.SetException(ex);
						throw;
					}
				}
			}

			private static string EtagToString(int eTag) => $"\"{eTag}\"";

			private IActionResult BuildResponseImpl(HttpRequest httpRequest, HttpResponse httpResponse)
			{
				httpRequest.Query["service.name"].Should().HaveCount(1);
				httpRequest.Query["service.name"].First().Should().Be(CustomServiceName);
				httpRequest.Query["service.environment"].Should().HaveCount(1);
				httpRequest.Query["service.environment"].First().Should().Be(CustomEnvironment);

				var eTag = EtagToString(_eTagAsInt);

				httpResponse.Headers[HeaderNames.CacheControl] = "must-revalidate, max-age=" + _waitUntilNextRequest.TotalSeconds;
				httpResponse.Headers[HeaderNames.ETag] = eTag;

				var containsCurrentEtag = httpRequest.Headers[HeaderNames.IfNoneMatch]
					.SelectMany(values => values.Split(",").Select(val => val.Trim()))
					.Contains(eTag);
				// ReSharper disable once InvertIf
				if (containsCurrentEtag)
				{
					_appliedByAgent.TrySetResult(null);
					return new StatusCodeResult((int)HttpStatusCode.NotModified);
				}

				return new ObjectResult(JsonConvert.SerializeObject(_options)) { StatusCode = (int)HttpStatusCode.OK };
			}
		}
	}
}
